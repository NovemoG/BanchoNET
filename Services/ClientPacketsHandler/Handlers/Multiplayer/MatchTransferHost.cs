﻿using BanchoNET.Models.Mongo;
using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;
using Action = BanchoNET.Models.Mongo.Action;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private async Task MatchTransferHost(Player player, BinaryReader br)
	{
		var slotId = br.ReadInt32();

		var lobby = player.Lobby;
		if (lobby == null) return;
		if (player.Id != lobby.HostId) return;
		if (slotId is < 0 or > 15) return;

		var target = lobby.Slots[slotId].Player;
		if (target == null) return;

		lobby.HostId = target.Id;

		using var hostTransferPacket = new ServerPackets();
		hostTransferPacket.MatchTransferHost();
		target.Enqueue(hostTransferPacket.GetContent());
		lobby.EnqueueState();

		await histories.AddMatchAction(
			lobby.LobbyId,
			new ActionEntry
			{
				Action = Action.HostChanged,
				PlayerId = target.Id,
				Date = DateTime.Now
			});
	}
}