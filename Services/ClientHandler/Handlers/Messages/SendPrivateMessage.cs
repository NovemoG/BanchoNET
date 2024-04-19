using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;

namespace BanchoNET.Services;

public partial class PacketsHandler
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
				//TODO command handling
			}
			else
			{
				//TODO np handling
			}
		}
		
		player.LastActivityTime = DateTime.Now;
	}
}