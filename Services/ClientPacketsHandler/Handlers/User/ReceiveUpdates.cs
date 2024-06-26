﻿using BanchoNET.Objects.Players;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task ReceiveUpdates(Player player, BinaryReader br)
	{
		var value = br.ReadInt32();
		
		player.PresenceFilter = (PresenceFilter)value;
		player.LastActivityTime = DateTime.Now;
		return Task.CompletedTask;
	}
}