using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private async Task SendPublicMessage(Player player, BinaryReader br)
	{
		var message = br.ReadOsuMessage();

		if (player.Silenced) return;

		var txt = message.Content.Trim();
		if (txt == string.Empty) return;
		
		if (_ignoredChannels.Contains(message.Destination)) return;
		
		Channel? channel;
		switch (message.Destination)
		{
			case "#spectator":
			{
				int spectatorId;

				if (player.IsSpectating) spectatorId = player.Spectating!.Id;
				else if (player.HasSpectators) spectatorId = player.Id;
				else return;
			
				channel = _session.GetChannel($"#s_{spectatorId}", true);
				break;
			}
			case "#multiplayer" when player.Lobby == null:
				return;
			case "#multiplayer":
				channel = player.Lobby.Chat;
				break;
			default:
				channel = _session.GetChannel(message.Destination);
				break;
		}

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

		if (txt.StartsWith(AppSettings.CommandPrefix))
		{
			var response = await commands.Execute(txt, player, channel);
			
			//TODO maybe show responses in multiplayer lobby for everyone?
			player.SendBotMessage(response, channel.Name);
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
			
			channel.SendMessage(new Message
			{
				Sender = player.Username,
				Content = txt,
				Destination = message.Destination,
				SenderId = player.Id
			});
		}
		
		player.LastActivityTime = DateTime.Now;
	}
}