using BanchoNET.Objects.Players;
using BanchoNET.Utils;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task PartMatch(Player player, BinaryReader br)
	{
		player.LeaveMatch();
		player.LastActivityTime = DateTime.Now;
		return Task.CompletedTask;
	}
}