using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private async Task SendPrivateMessage(Player player, BinaryReader br)
	{
		var message = br.ReadOsuMessage();

		if (player.Silenced) return;

		var txt = message.Content.Trim();
		if (txt == string.Empty) return;

		var target = await players.GetPlayerOrOffline(message.Destination);
		if (target == null) return;
		if (player.BlockedByPlayer(target.Id))
		{
			using var dmBlockedPacket = new ServerPackets();
			dmBlockedPacket.UserDmBlocked(target.Username);
			player.Enqueue(dmBlockedPacket.GetContent());
			return;
		}
		
		if (target.PmFriendsOnly && !target.Friends.Contains(player.Id))
		{
			using var dmBlockedPacket = new ServerPackets();
			dmBlockedPacket.UserDmBlocked(target.Username);
			player.Enqueue(dmBlockedPacket.GetContent());
			return;
		}

		if (target.Silenced)
		{
			using var silencedPacket = new ServerPackets();
			silencedPacket.TargetSilenced(target.Username);
			player.Enqueue(silencedPacket.GetContent());
			return;
		}
		
		if (txt.Length > 2000)
		{
			txt = $"{txt[..2000]}... (truncated)";

			using var msgPacket = new ServerPackets();
			msgPacket.Notification("Your message was too long and has been truncated.\n(Exceeded 2000 characters)");
			player.Enqueue(msgPacket.GetContent());
		}

		if (target.Status.Activity == Activity.Afk && !string.IsNullOrEmpty(target.AwayMessage))
			player.SendMessage(target.AwayMessage, target);

		if (_session.Bots.FirstOrDefault(b => b.Username == target.Username) == null)
		{
			if (target.Online)
				target.SendMessage(txt, player);
			else
			{
				using var msgPacket = new ServerPackets();
				msgPacket.Notification($"{target.Username} is currently offline but will\nreceive your message on their next login.");
				player.Enqueue(msgPacket.GetContent());
			}
			
			//TODO add to db
		}
		else
		{
			if (txt.StartsWith(AppSettings.CommandPrefix))
			{
				var command = await commands.Execute(txt, player);
			
				if (!string.IsNullOrEmpty(command.Response))
					player.SendMessage(command.Response, target);
			}
			else
			{
				var npMatch = Regexes.NowPlaying.Match(txt);

				if (npMatch.Success)
				{
					var modeGroup = npMatch.Groups["mode_vn"];

					player.LastNp = new LastNp
					{
						BeatmapId = int.Parse(npMatch.Groups["bid"].Value),
						SetId = int.Parse(npMatch.Groups["sid"].Value),
						Mode = modeGroup.Success
							? modeGroup.Value.FromRegexMatch()
							: player.Status.Mode
					};
				}
				
				//TODO pp values response
			}
		}
		
		player.LastActivityTime = DateTime.Now;
	}
}