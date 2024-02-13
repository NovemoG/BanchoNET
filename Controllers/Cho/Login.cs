using System.Text;
using BanchoNET.Objects;
using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Players;
using BanchoNET.Objects.Privileges;
using BanchoNET.Packets;
using BanchoNET.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace BanchoNET.Controllers.Cho;

public partial class ChoController
{
	private const string OsuApiV2ChangelogUrl = "https://osu.ppy.sh/api/v2/changelog";
	
	private async Task<IActionResult> Login()
	{
		using var stream = new MemoryStream();
		
		await Request.Body.CopyToAsync(stream);
		var rawBody = stream.ToArray();

		LoginData? loginData;
		try
		{
			var bodyString = Encoding.UTF8.GetString(rawBody).Split("\n", 3);
			
			loginData = ParseLoginData(bodyString);

			if (loginData == null)
			{
				Response.Headers["cho-token"] = "invalid-request";
				return BadRequest();
			}
		}
		catch
		{
			Response.Headers["cho-token"] = "invalid-request";

			using var responseData = new ServerPackets();
			responseData.Notification("Error occurred");
			responseData.PlayerId(-5);
			return responseData.GetContentResult();
		}

		if (_config.DisallowOldClients)
		{
			var clientStream = loginData.OsuVersion.Stream;
			if (clientStream is "stable" or "beta") clientStream += "40";

			//TODO cache latest major version once every day
			//TODO this changelog doesnt provide version info for tourney/dev client
			
			var response = await _httpClient.GetAsync($"{OsuApiV2ChangelogUrl}?stream={clientStream}");
			response.EnsureSuccessStatusCode();

			dynamic changelog = JObject.Parse(await response.Content.ReadAsStringAsync());

			var latestMajorVersion = loginData.OsuVersion.Date;
			foreach (var build in changelog.builds)
			{
				latestMajorVersion = DateTime.ParseExact(build.version.ToString().Substring(0, 8), "yyyyMMdd", null);

				if (((IEnumerable<dynamic>)build.changelog_entries).Any(entry => (bool)entry.major)) break;
			}

			if (loginData.OsuVersion.Date < latestMajorVersion)
			{
				Response.Headers["cho-token"] = "client-too-old";
                
                using var responseData = new ServerPackets();
                responseData.VersionUpdate();
                responseData.PlayerId(-2);
                return responseData.GetContentResult();
			}
		}

		var runningUnderWine = loginData.AdaptersString == "runningunderwine";
		var adapters = loginData.AdaptersString[..^1].Split('.');
		
		if (!(runningUnderWine || adapters.Length != 0))
		{
			Response.Headers["cho-token"] = "empty-adapters";
                
			using var responseData = new ServerPackets();
			responseData.PlayerId(-1);
			responseData.Notification("Please restart your osu! client and try again.");
			return responseData.GetContentResult();
		}

		var player = _session.GetPlayer(username: loginData.Username);
		if (player != null && loginData.OsuVersion.Stream != "tourney")
		{
			Console.WriteLine($"[{GetType().Name}] Login time difference: {DateTime.UtcNow - player.LastActivityTime}");
			if (DateTime.UtcNow - player.LastActivityTime < TimeSpan.FromSeconds(15))
			{
				Response.Headers["cho-token"] = "user-already-logged-in";
                
				using var responseData = new ServerPackets();
				responseData.PlayerId(-1);
				responseData.Notification("User already logged in.");
				return responseData.GetContentResult();
			}

			_session.LogoutPlayer(player);
		}
		
		var userInfo = await _bancho.FetchPlayerInfo(loginName: loginData.Username);
		if (userInfo == null)
		{
			Response.Headers["cho-token"] = "unknown-username";
                
			using var responseData = new ServerPackets();
			responseData.Notification("Unknown username.");
			responseData.PlayerId(-1);
			return responseData.GetContentResult();
		}
		
		var privileges = (Privileges)userInfo.Privileges;
		if (loginData.OsuVersion.Stream == "tourney" &&
		    !privileges.HasPrivilege(Privileges.Supporter) &&
		    privileges.HasPrivilege(Privileges.Unrestricted))
		{
			Response.Headers["cho-token"] = "no";
                
			using var responseData = new ServerPackets();
			responseData.PlayerId(-1);
			return responseData.GetContentResult();
		}
		
		if (!_session.CheckHashes(loginData.PasswordMD5, userInfo.PasswordHash))
		{
			Response.Headers["cho-token"] = "incorrect-password";
				
			using var responseData = new ServerPackets();
			responseData.Notification("Incorrect password.");
			responseData.PlayerId(-1);
			return responseData.GetContentResult();
		}
		
		//TODO add login data and client hashes to database
		//TODO hw matches
		//TODO geoloc
		player = new Player(userInfo, Guid.NewGuid(), DateTime.UtcNow, 1)
		{
			Geoloc = new Geoloc
			{
				Country = new Country
				{
					Acronym = "pl",
					Numeric = 10,
				},
				Longitude = 6.9f,
				Latitude = 7.27f
			}
		};

		using var loginPackets = new ServerPackets();

		loginPackets.ProtocolVersion(19);
		loginPackets.PlayerId(player.Id);
		loginPackets.BanchoPrivileges((int)player.ToBanchoPrivileges());
		loginPackets.Notification(_config.WelcomeMessage);
		loginPackets.ChannelInfo(_session.GetAutoJoinChannels(player));
		loginPackets.ChannelInfoEnd();
		loginPackets.MainMenuIcon(_config.MenuIconUrl, _config.MenuOnclickUrl);
		
		await _bancho.FetchPlayerStats(player);
		await _bancho.FetchPlayerRelationships(player);

		loginPackets.FriendsList(player.Friends);
		loginPackets.SilenceEnd(player.RemainingSilence);
		loginPackets.UserPresence(player);
		loginPackets.UserStats(player);

		var banchoBot = _session.BanchoBot;
		if (!player.Restricted)
		{
			loginPackets.OtherPlayers(player);
			
			//TODO check for offline messages
			
			if (!player.Privileges.HasPrivilege(Privileges.Verified))
			{
				await _bancho.AddPlayerPrivileges(player, Privileges.Verified);

				loginPackets.SendMessage(new Message
				{
					Sender = banchoBot.Username,
					Content = _config.WelcomeMessage,
					Destination = player.Username,
					SenderId = banchoBot.Id
				});
			}
		}
		else
		{
			loginPackets.OtherPlayers();
			loginPackets.AccountRestricted();
			loginPackets.SendMessage(new Message
			{
				Sender = banchoBot.Username,
				Content = _config.RestrictedMessage,
				Destination = player.Username,
				SenderId = banchoBot.Id
			});
		}
		
		_session.AppendPlayer(player);
		
		//TODO note some statistics maybe?
		
		//TODO logger message
		Console.WriteLine($"[Login] {player.Username} Logged in");
		
		Response.Headers["cho-token"] = player.Token.ToString();
		
		return loginPackets.GetContentResult();
	}

	private LoginData? ParseLoginData(IReadOnlyList<string> bodyString)
	{
		var remainder = bodyString[2].Split('|', 5);
		
		var versionMatch = Regexes.OsuVersion.Match(remainder[0]);

		if (!versionMatch.Success) return null;
			
		var osuVersion = new OsuVersion
		{
			Date = DateTime.ParseExact(versionMatch.Groups["date"].Value, "yyyyMMdd", null),
			Revision = versionMatch.Groups["revision"].Success
				? int.Parse(versionMatch.Groups["revision"].Value) 
				: 0,
			Stream = versionMatch.Groups["stream"].Success
				? versionMatch.Groups["stream"].Value
				: "stable"
		};
		
		var clientHashes = remainder[3][..^1].Split(':', 5);
		
		return new LoginData
		{
			Username = bodyString[0],
			PasswordMD5 = bodyString[1],
			OsuVersion = osuVersion,
			TimeZone = byte.Parse(remainder[1]),
			DisplayCity = remainder[2] == "1",
			PmPrivate = remainder[4] == "1",
			OsuPathMD5 = clientHashes[0],
			AdaptersString = clientHashes[1],
			AdaptersMD5 = clientHashes[2],
			UninstallMD5 = clientHashes[3],
			DiskSignatureMD5 = clientHashes[4]
		};
	}
}