using System.Text;
using BanchoNET.Models;
using BanchoNET.Objects.Other;
using BanchoNET.Objects.Privileges;
using BanchoNET.Packets.Server;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Controllers.Cho;

public partial class ChoController
{
	private async Task<ActionResult> Login()
	{
		using var stream = new MemoryStream();
		
		await Request.Body.CopyToAsync(stream);
		var rawBody = stream.ToArray();

		try
		{
			var bodyString = Encoding.UTF8.GetString(rawBody).Split("\n", 3);
			var username = bodyString[0];
			var passwordMD5 = bodyString[1];
			var remainder = bodyString[2].Split('|', 5);

			var osuVersion = remainder[0];
			var utcOffset = remainder[1];
			var displayCity = remainder[2];
			var clientHashes = remainder[3].Split(':', 5);
			var pmPrivate = remainder[4];

			var osuPathMD5 = clientHashes[0];
			var adaptersString = clientHashes[1];
			var adaptersMD5 = clientHashes[2];
			var uninstallMD5 = clientHashes[3];
			var diskSignatureMD5 = clientHashes[4];
			
			Console.WriteLine($"Username: {username}");
		}
		catch
		{
			//TODO
		}

		var player = new Player
		{
			Id = 3,
			Username = "Cossin",
			PasswordHash = "",
			TimeZone = 1,
			Country = "Poland",
			LastConnectionTime = DateTime.Now,
			Privileges = 0,
			Restricted = false,
			RemainingSilence = 0,
			Rank = 1,
			Longitude = 1.1f,
			Latitude = 1.1f,
			Friends = [],
			BotClient = false,
			Token = new Guid().ToString(),
		};

		using var responseData = new ServerPacket();

		responseData.ProtocolVersion(19);
		responseData.UserId(player.Id);
		responseData.BanchoPrivileges(player.Privileges);
		responseData.WelcomeMessage(Utils.Packets.WelcomeMessage);
		responseData.ChannelInfo(bancho.GetAutoJoinChannels(player));
		responseData.ChannelInfoEnd();
		
		//TODO Fetch player stats
		//TODO Fetch player friends
		
		responseData.MainMenuIcon(Utils.Packets.MenuIconUrl, Utils.Packets.MenuOnclickUrl);
		responseData.FriendsList(player.Friends);
		responseData.SilenceEnd(player.RemainingSilence);
		responseData.UserPresence(player);
		responseData.UserStats(player);

		if (!player.Restricted)
		{
			//TODO send information to other players that this player just logged on and get info about other players
			
			//TODO check for offline messages

			if ((player.Privileges & (int)Privileges.VERIFIED) == 0)
			{
				//TODO add privileges
				
				responseData.SendMessage(new Message
				{
					Sender = "Bancho", //TODO get bancho bot name
					Content = Utils.Packets.WelcomeMessage, //TODO load from config
					Destination = player.Username,
					SenderId = 1 //TODO get bancho bot id
				});
			}
		}
		else
		{
			//TODO get info about other players
			
			responseData.AccountRestricted();
			responseData.SendMessage(new Message
			{
				Sender = "Bancho", //TODO get bancho bot name
				Content = Utils.Packets.RestrictedMessage, //TODO load from config
				Destination = player.Username,
				SenderId = 1 //TODO get bancho bot id
			});
		}
		
		bancho.AppendPlayerSession(player);
		
		//TODO note some statistics maybe?
		
		//TODO logger message
		Console.WriteLine($"{player.Username} Logged in");
		
		responseData.WriteToResponse(Response);
		
		Console.WriteLine($"Response Body: {Response.Body.Length}");

		Response.Headers["cho-token"] = player.Token;
		return Ok();
		
		/*if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(passwordMD5))
		{
			return BadRequest();
		}

		var player = services.GetPlayerSession(username);

		if (player == null)
		{
			return BadRequest();
		}

		if (!player.CheckPassword(passwordMD5))
		{
			return Unauthorized();
		}*/
	}
}