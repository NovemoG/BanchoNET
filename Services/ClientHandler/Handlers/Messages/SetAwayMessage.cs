using BanchoNET.Objects.Players;
using BanchoNET.Utils;

namespace BanchoNET.Services;

public partial class BanchoHandler
{
	private Task SetAwayMessage(Player player, BinaryReader br)
	{
		var message = br.ReadOsuString();
		player.AwayMessage = message;
		return Task.CompletedTask;
	}
}