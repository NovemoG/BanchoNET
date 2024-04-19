using BanchoNET.Objects.Players;
using BanchoNET.Utils;

namespace BanchoNET.Services;

public partial class PacketsHandler
{
	private Task SetAwayMessage(Player player, BinaryReader br)
	{
		var message = br.ReadOsuString();
		player.AwayMessage = message;
		return Task.CompletedTask;
	}
}