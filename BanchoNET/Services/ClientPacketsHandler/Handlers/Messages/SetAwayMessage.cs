using BanchoNET.Core.Models.Users;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task SetAwayMessage(User player, BinaryReader br)
	{
		var message = br.ReadOsuString();
		player.AwayMessage = message;
		return Task.CompletedTask;
	}
}