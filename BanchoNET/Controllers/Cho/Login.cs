using System.Text;
using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Privileges;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.Cho;

public partial class ChoController
{
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

			return new ServerPackets()
				.Notification("Error occured")
				.PlayerId(-5)
				.FinalizeAndGetContentResult();
		}
		
		if (!AppSettings.DisallowOldClients)
		{
			var clientStream = loginData.OsuVersion.Stream;
			if (clientStream is "beta")
			{
				Response.Headers["cho-token"] = "client-too-old";

				return new ServerPackets()
					.VersionUpdate()
					.PlayerId(-2)
					.FinalizeAndGetContentResult();
			}
			if (clientStream is "stable") clientStream += "40";
			
			//TODO this changelog doesnt provide version info for tourney/dev client

			logger.LogDebug($"[Login] Login osu version: {loginData.OsuVersion.Date}, server osu version: {version.GetLatestVersion(clientStream).Date}");
			
			if (loginData.OsuVersion > version.GetLatestVersion(clientStream))
			{
				Response.Headers["cho-token"] = "client-too-old";

				return new ServerPackets()
					.VersionUpdate()
					.PlayerId(-2)
					.FinalizeAndGetContentResult();
			}
		}

		var runningUnderWine = loginData.AdaptersString == "runningunderwine";
		var adapters = loginData.AdaptersString[..^1].Split('.');
		
		if (!(runningUnderWine || adapters.Length != 0))
		{
			Response.Headers["cho-token"] = "empty-adapters";

			return new ServerPackets()
				.Notification("Please restart your osu! client and try again.")
				.PlayerId(-1)
				.FinalizeAndGetContentResult();
		}

		var player = playerService.GetPlayer(loginData.Username);
		if (player != null && loginData.OsuVersion.Stream != "tourney")
		{
			logger.LogDebug($"Login time difference: {DateTime.UtcNow - player.LastActivityTime}");
			
			if (DateTime.UtcNow - player.LastActivityTime < TimeSpan.FromSeconds(15))
			{
				Response.Headers["cho-token"] = "user-already-logged-in";

				return new ServerPackets()
					.Notification("User already logged in.")
					.PlayerId(-1)
					.FinalizeAndGetContentResult();
			}

			playerCoordinator.LogoutPlayer(player);
		}
		
		var userInfo = await players.GetPlayerInfoFromLogin(loginData.Username);
		if (userInfo == null)
		{
			Response.Headers["cho-token"] = "unknown-username";

			return new ServerPackets()
				.Notification("Unknown username.")
				.PlayerId(-1)
				.FinalizeAndGetContentResult();
		}
		
		var privileges = (PlayerPrivileges)userInfo.Privileges;
		if (loginData.OsuVersion.Stream == "tourney" &&
		    !privileges.HasPrivilege(PlayerPrivileges.Supporter) &&
		    privileges.HasPrivilege(PlayerPrivileges.Unrestricted))
		{
			Response.Headers["cho-token"] = "no";

			return new ServerPackets()
				.PlayerId(-1)
				.FinalizeAndGetContentResult();
		}
		
		if (!loginData.PasswordMD5.VerifyPassword(userInfo.PasswordHash))
		{
			Response.Headers["cho-token"] = "incorrect-password";

			return new ServerPackets()
				.Notification("Incorrect password.")
				.PlayerId(-1)
				.FinalizeAndGetContentResult();
		}

		await clients.InsertLoginData(
			userInfo.Id,
			geoloc.GetIp(Request.Headers),
			loginData.OsuVersion.Date,
			loginData.OsuVersion.Stream);

		var bannedUsersHashes = await clients.TryInsertClientHashes(userInfo.Id,
			loginData.OsuPathMD5,
			loginData.AdaptersMD5,
			loginData.UninstallMD5,
			loginData.DiskSignatureMD5,
			runningUnderWine);

		var sendHashWarning = false;
		if (bannedUsersHashes)
		{
			if (((PlayerPrivileges)userInfo.Privileges).HasPrivilege(PlayerPrivileges.Verified))
				sendHashWarning = true;
			else
			{
				Response.Headers["cho-token"] = "contact-staff";

				return new ServerPackets()
					.Notification("Please contact staff directly to create an account.")
					.PlayerId(-1)
					.FinalizeAndGetContentResult();
			}
		}
		//TODO assign club
		
		var _geoloc = await geoloc.GetGeoloc(Request.Headers);
		if (_geoloc == null)
		{
			Response.Headers["cho-token"] = "login-failed";

			return new ServerPackets()
				.Notification("Login failed. Please contact an admin.")
				.PlayerId(-1)
				.FinalizeAndGetContentResult();
		}

		player = new User(userInfo, timeZone: loginData.TimeZone)
		{
			Geoloc = _geoloc.Value,
			ClientDetails = new ClientDetails
			{
				OsuVersion = loginData.OsuVersion,
				OsuPathMD5 = loginData.OsuPathMD5,
				AdaptersMD5 = loginData.AdaptersMD5,
				UninstallMD5 = loginData.UninstallMD5,
				DiskSignatureMD5 = loginData.DiskSignatureMD5,
				Adapters = loginData.AdaptersString.Split('.')[..^1].ToList(),
				IpAddress = geoloc.GetIp(Request.Headers)
			}
		};

		if (userInfo.Country == "xx")
			await players.UpdatePlayerCountry(player, _geoloc.Value.Country.Acronym);

		using var loginPackets = new ServerPackets()
			.ProtocolVersion(19)
			.PlayerId(player.Id)
			.BanchoPrivileges((int)(player.ToBanchoPrivileges() | ClientPrivileges.Supporter))
			.Notification(AppSettings.WelcomeMessage);

		#region Append AutoJoin Channels

		foreach (var channel in channels.Channels)
		{
			if (!channel.AutoJoin || 
			    !channel.CanPlayerRead(player) ||
			    channel.IdName == "#lobby")
			{
				continue;
			}

			var chanInfoBytes = new ServerPackets()
				.ChannelInfo(channel)
				.FinalizeAndGetContent();
			
			loginPackets.WriteBytes(chanInfoBytes);
			
			foreach (var sessionPlayer in playerService.Players)
				if (channel.CanPlayerRead(sessionPlayer))
					sessionPlayer.Enqueue(chanInfoBytes);
		}
		
		loginPackets.ChannelInfoEnd();

		#endregion
		
		loginPackets.MainMenuIcon(AppSettings.MenuIconUrl, AppSettings.MenuOnclickUrl);
		
		await players.GetPlayerStats(player);
		await players.GetPlayerRelationships(player);

		loginPackets.FriendsList(player.Friends)
			.SilenceEnd(Math.Max(0, (int)(player.RemainingSilence - DateTime.UtcNow).TotalSeconds))
			.UserPresence(player)
			.UserStats(player);

		var banchoBot = playerService.BanchoBot;
		if (!player.IsRestricted)
		{
			EnqueueOtherPlayers(loginPackets, player);
			
			if (!player.Privileges.HasPrivilege(PlayerPrivileges.Verified))
			{
				await players.UpdatePlayerPrivileges(player, PlayerPrivileges.Verified, false);

				loginPackets.SendMessage(new Message
				{
					Sender = banchoBot.Username,
					Content = AppSettings.FirstLoginMessage,
					Destination = player.Username,
					SenderId = banchoBot.Id
				});
			}
		}
		else
		{
			EnqueueOtherPlayers(loginPackets)
				.AccountRestricted()
				.SendMessage(new Message
				{
					Sender = banchoBot.Username,
					Content = AppSettings.RestrictedMessage,
					Destination = player.Username,
					SenderId = banchoBot.Id
				});
		}

		if (sendHashWarning)
		{
			loginPackets.SendMessage(new Message
			{
				Sender = banchoBot.Username,
				Content = "Your client hashes are associated with another account. Please be careful as it may lead to a ban.",
				Destination = player.Username,
				SenderId = banchoBot.Id
			});
		}
		
		playerService.InsertPlayer(player);
		
		var unreadMessages = await messages.GetUnreadMessages(player.Id);
		if (unreadMessages.Count > 0)
		{
			loginPackets.Notification("You have unread messages. Please check them in the chat.");
			
			foreach (var message in unreadMessages)
			{
				loginPackets.SendMessage(new Message
				{
					Sender = message.Sender.Username,
					Content = message.Message,
					Destination = player.Username,
					SenderId = message.SenderId
				});
				await messages.MarkMessageAsRead(message.Id);
			}
		}
		
		logger.LogDebug($"[Login] {player.Username} Logged in");
		Response.Headers["cho-token"] = player.SessionId.ToString();
		
		return loginPackets.GetContentResult();
	}

	private static LoginData? ParseLoginData(string[] bodyString)
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
			TimeZone = sbyte.Parse(remainder[1]),
			DisplayCity = remainder[2] == "1",
			PmPrivate = remainder[4] == "1",
			OsuPathMD5 = clientHashes[0],
			AdaptersString = clientHashes[1],
			AdaptersMD5 = clientHashes[2],
			UninstallMD5 = clientHashes[3],
			DiskSignatureMD5 = clientHashes[4]
		};
	}
	
	/// <summary>
	/// Enqueues data of other players to player's buffer and if specified it also provides player's data to other players
	/// </summary>
	/// <param name="packets">Packets to which data will be enqueued</param>
	/// <param name="player">Player from which data will be enqueued to others</param>
	private ServerPackets EnqueueOtherPlayers(ServerPackets packets, User? player = null)
	{
		var toOthers = player != null;
		
		using var playerLogin = new ServerPackets();
		if (toOthers)
		{
			playerLogin.UserPresence(player!);
			playerLogin.UserStats(player!);
		}
		var loginData = playerLogin.GetContent();
		
		foreach (var bot in playerService.Bots)
		{
			packets.BotPresence(bot);
			packets.BotStats(bot);
		}
		
		foreach (var user in playerService.Players)
		{
			if (toOthers) user.Enqueue(loginData);
			packets.UserPresence(user);
			packets.UserStats(user);
		}

		if (!toOthers) return packets;
		
		foreach (var restricted in playerService.Restricted)
			restricted.Enqueue(loginData);

		return packets;
	}
}