using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private async Task SendPublicMessage(User player, BinaryReader br)
	{
		var message = br.ReadOsuMessage();

		if (player.IsSilenced) return;

		var txt = message.Content.Trim();
		if (txt == string.Empty) return;
		
		if (IgnoredChannels.Contains(message.Destination)) return;
		
		Channel? channel;
		switch (message.Destination)
		{
			case "#spectator":
			{
				int spectatorId;

				if (player.IsSpectating) spectatorId = player.Spectating!.Id;
				else if (player.HasSpectators) spectatorId = player.Id;
				else return;
			
				channel = channels.GetChannel($"#s_{spectatorId}");
				break;
			}
			case "#multiplayer" when player.Match == null:
				return;
			case "#multiplayer":
				channel = player.Match.Chat;
				break;
			default:
				channel = channels.GetChannel(message.Destination);
				break;
		}

		if (channel == null)
			return;

		if (!channel.PlayerInChannel(player))
			return;

		if (!channel.CanPlayerWrite(player))
			return;

		if (txt.Length > 2000)
		{
			txt = $"{txt[..2000]}... (truncated)";

			player.Enqueue(new ServerPackets()
				.Notification("Your message was too long and has been truncated.\n(Exceeded 2000 characters)")
				.FinalizeAndGetContent());
		}

		if (txt.StartsWith(AppSettings.CommandPrefix))
			await SendCommandMessage(txt, message, player, channel);
		else
			SendNpMessage(txt, message, player, channel);
		
		player.LastActivityTime = DateTime.UtcNow;
	}

	private async Task SendCommandMessage(
		string txt,
		Message message,
		User player,
		Channel channel
	) {
		var command = await commands.Execute(txt, player, channel);
			
		if (!string.IsNullOrEmpty(command.Response))
		{
			if (command.ToPlayer)
				playerService.SendBotMessageTo(player, command.Response, channel.Name);
			else
			{
				channels.SendMessageTo(channel, new Message
				{
					Sender = player.Username,
					Content = txt,
					Destination = message.Destination,
					SenderId = player.Id
				});
				channels.SendBotMessageTo(channel, command.Response, playerService.BanchoBot);
			}
		}
	}

	private void SendNpMessage(
		string txt,
		Message message,
		User player,
		Channel channel
	) {
		var npMatch = Regexes.NowPlaying.Match(txt);

		if (npMatch.Success)
		{
			var modeGroup = npMatch.Groups["mode_vn"];

			//TODO store beatmap instead if used more than once
			player.LastNp = new LastNp
			{
				BeatmapId = int.Parse(npMatch.Groups["bid"].Value),
				SetId = int.Parse(npMatch.Groups["sid"].Value),
				Mode = modeGroup.Success
					? modeGroup.Value.FromRegexMatch()
					: player.Status.Mode
			};
		}
		
		channels.SendMessageTo(channel, new Message
		{
			Sender = player.Username,
			Content = txt,
			Destination = message.Destination,
			SenderId = player.Id
		});
	}
}