using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Players;
using BanchoNET.Utils;

namespace BanchoNET.Packets;

public partial class ClientPackets
{
	public static void SendPublicMessage(Player player, BinaryReader br)
	{
		var message = br.ReadOsuMessage();

		if (player.Silenced) return;

		var txt = message.Content.Trim();
		if (txt == string.Empty) return;
		
		if (IgnoredChannels.Contains(message.Destination)) return;

		Channel? channel;
		if (message.Destination == "#spectator")
		{
			int spectatorId;

			if (player.IsSpectating) spectatorId = player.Spectating!.Id;
			else if (player.HasSpectators) spectatorId = player.Id;
			else return;
			
			channel = Session.GetChannel($"#s_{spectatorId}", ChannelType.Spectator)!;
		}
		else if (message.Destination == "#multiplayer")
		{
			if (player.Lobby == null) return;

			channel = player.Lobby.Chat;
		}
		else
			channel = Session.GetChannel(message.Destination);

		if (channel == null) //TODO log
			return;

		if (!channel.PlayerInChannel(player))
			return;

		if (!channel.CanPlayerWrite(player))
			return;

		if (txt.Length > 2000)
		{
			txt = $"{txt[..2000]}... (truncated)";

			using var msgPacket = new ServerPackets();
			msgPacket.Notification("Your message was too long and has been truncated.\n(Exceeded 2000 characters)");
			player.Enqueue(msgPacket.GetContent());
		}

		if (txt.StartsWith('!')) //Load from config
		{
			//TODO command
		}
		else
		{
			var npMatch = Regexes.NowPlaying().Match(txt);

			if (npMatch.Success)
			{
				//TODO load map
				
				//TODO maybe store np in player instance (?)
			}
			
			channel.SendMessage(new Message
			{
				Sender = player.Username,
				Content = txt,
				Destination = message.Destination,
				SenderId = player.Id
			});
		}
		
		player.UpdateLatestActivity();
	}
}