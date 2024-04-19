﻿using BanchoNET.Objects.Multiplayer;
using BanchoNET.Objects.Players;
using BanchoNET.Utils;

namespace BanchoNET.Services;

public partial class PacketsHandler
{
	private Task MatchNotReady(Player player, BinaryReader br)
	{
		var lobby = player.Lobby;
		if (lobby == null) return Task.CompletedTask;

		var slot = lobby.GetPlayerSlot(player);
		slot.Status = SlotStatus.NotReady;
		
		lobby.EnqueueState(false);
		return Task.CompletedTask;
	}
}