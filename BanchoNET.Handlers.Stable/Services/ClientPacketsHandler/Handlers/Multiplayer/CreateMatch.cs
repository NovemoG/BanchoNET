using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Packets;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Handlers.Stable.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private async Task CreateMatch(Player player, BinaryReader br)
	{
		var matchData = br.ReadOsuMatch();
		matchData.CreatorId = matchData.HostId;
		
		if (player.IsRestricted)
		{
			player.Enqueue(new ServerPackets()
				.MatchJoinFail()
				.Notification("Multiplayer is not available while restricted.")
				.FinalizeAndGetContent());
			return;
		}

		if (player.IsSilenced)
		{
			player.Enqueue(new ServerPackets()
				.MatchJoinFail()
				.Notification("Multiplayer is not available while silenced.")
				.FinalizeAndGetContent());
			return;
		}

		if (await multiplayerCoordinator.CreateMatchAsync(matchData, player))
			player.LastActivityTime = DateTime.UtcNow;
	}
}