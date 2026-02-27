using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task MatchChangePassword(User player, BinaryReader br)
	{
		var matchData = br.ReadOsuMatch();

		var match = player.Match;
		if (match == null || player.Id != match.HostId) return Task.CompletedTask;

		match.Password = matchData.Password;
		multiplayerCoordinator.EnqueueStateTo(match);
		
		return Task.CompletedTask;
	}
}