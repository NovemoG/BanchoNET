using BanchoNET.Objects.Players;
using BanchoNET.Utils;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task SetAwayMessage(Player player, BinaryReader br)
	{
		var message = br.ReadOsuString();
		player.AwayMessage = message;
		return Task.CompletedTask;
	}
}