using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;

namespace BanchoNET.Services;

public partial class BanchoHandler
{
	private Task SendPublicMessage(Player player, BinaryReader br)
	{
		var message = br.ReadOsuMessage();

		if (player.Silenced) return Task.CompletedTask;

		var txt = message.Content.Trim();
		if (txt == string.Empty) return Task.CompletedTask;
		
		if (_ignoredChannels.Contains(message.Destination)) return Task.CompletedTask;

		Channel? channel;
		if (message.Destination == "#spectator")
		{
			int spectatorId;

			if (player.IsSpectating) spectatorId = player.Spectating!.Id;
			else if (player.HasSpectators) spectatorId = player.Id;
			else return Task.CompletedTask;
			
			channel = _session.GetChannel($"#s_{spectatorId}", true)!;
		}
		else if (message.Destination == "#multiplayer")
		{
			if (player.Lobby == null) return Task.CompletedTask;

			channel = player.Lobby.Chat;
		}
		else
			channel = _session.GetChannel(message.Destination);

		if (channel == null) //TODO log
			return Task.CompletedTask;

		if (!channel.PlayerInChannel(player))
			return Task.CompletedTask;

		if (!channel.CanPlayerWrite(player))
			return Task.CompletedTask;

		if (txt.Length > 2000)
		{
			txt = $"{txt[..2000]}... (truncated)";

			using var msgPacket = new ServerPackets();
			msgPacket.Notification("Your message was too long and has been truncated.\n(Exceeded 2000 characters)");
			player.Enqueue(msgPacket.GetContent());
		}

		if (txt.StartsWith(AppSettings.CommandPrefix))
		{
			//TODO command hadnling
		}
		else
		{
			var npMatch = Regexes.NowPlaying.Match(txt);

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
		
		player.LastActivityTime = DateTime.UtcNow;
		return Task.CompletedTask;
	}
}