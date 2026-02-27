using BanchoNET.Core.Models.Players;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task ReceiveUpdates(User player, BinaryReader br)
	{
		var value = br.ReadInt32();
		
		player.Presence = (PresenceFilter)value;
		player.LastActivityTime = DateTime.UtcNow;
		return Task.CompletedTask;
	}
}