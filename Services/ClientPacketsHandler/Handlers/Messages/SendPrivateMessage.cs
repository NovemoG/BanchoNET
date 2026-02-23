using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;
using BanchoNET.Utils.Extensions;

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
			player.Enqueue(new ServerPackets()
				.UserDmBlocked(target.Username)
				.FinalizeAndGetContent());
			return;
		}

		if (!target.Online)
			await players.GetPlayerRelationships(target);
		
		if (target.PmFriendsOnly && !target.Friends.Contains(player.Id))
		{
			player.Enqueue(new ServerPackets()
				.UserDmBlocked(target.Username)
				.FinalizeAndGetContent());
			return;
		}

		if (target.Silenced)
		{
			player.Enqueue(new ServerPackets()
				.TargetSilenced(target.Username)
				.FinalizeAndGetContent());
			return;
		}
		
		if (txt.Length > 2000)
		{
			txt = $"{txt[..2000]}... (truncated)";
			
			player.Enqueue(new ServerPackets()
				.Notification("Your message was too long and has been truncated.\n(Exceeded 2000 characters)")
				.FinalizeAndGetContent());
		}

		if (target.Status.Activity == Activity.Afk && !string.IsNullOrEmpty(target.AwayMessage))
			player.SendMessage(target.AwayMessage, target);

		if (session.Bots.FirstOrDefault(b => b.Username == target.Username) == null)
		{
			var read = false;
			if (target.Online)
			{
				target.SendMessage(txt, player);
				read = true;
			}
			else
			{
				player.Enqueue(new ServerPackets()
					.Notification($"{target.Username} is currently offline but will\nreceive your message on their next login.")
					.FinalizeAndGetContent());
			}
			
			await messages.AddMessage(txt, player.Id, target.Id, read);
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