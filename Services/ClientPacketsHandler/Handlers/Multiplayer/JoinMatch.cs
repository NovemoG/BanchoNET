﻿using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;

namespace BanchoNET.Services.ClientPacketsHandler;

public partial class ClientPacketsHandler
{
	private Task JoinMatch(Player player, BinaryReader br)
	{
		var lobbyId = br.ReadInt32();
		var password = br.ReadOsuString();
		
		if (lobbyId is < 0 or > ushort.MaxValue)
			return Task.CompletedTask;
		
		if (player.Restricted)
		{
			using var joinFailPacket = new ServerPackets();
			joinFailPacket.MatchJoinFail();
			joinFailPacket.Notification("Multiplayer is not available while restricted.");
			player.Enqueue(joinFailPacket.GetContent());
			
			return Task.CompletedTask;
		}

		if (player.Silenced)
		{
			using var joinFailPacket = new ServerPackets();
			joinFailPacket.MatchJoinFail();
			joinFailPacket.Notification("Multiplayer is not available while silenced.");
			player.Enqueue(joinFailPacket.GetContent());
			
			return Task.CompletedTask;
		}
		
		var lobby = _session.GetLobby((ushort)lobbyId);
		if (lobby == null)
		{
			Console.WriteLine($"[JoinMatch] {player.Username} tried to join a non-existent lobby ({lobbyId})");
			
			using var joinFailPacket = new ServerPackets();
			joinFailPacket.MatchJoinFail();
			player.Enqueue(joinFailPacket.GetContent());
			return Task.CompletedTask;
		}

		if (lobby.BannedPlayers.Contains(player.Id))
		{
			using var joinFailPacket = new ServerPackets();
			joinFailPacket.MatchJoinFail();
			joinFailPacket.Notification("You are banned from joining this lobby.");
			player.Enqueue(joinFailPacket.GetContent());
			return Task.CompletedTask;
		}
		
		player.LastActivityTime = DateTime.Now;
		player.JoinMatch(lobby, password);

		player.SendBotMessage($"Match created by {player.Username} {lobby.MPLinkEmbed()}", "#multiplayer");
		return Task.CompletedTask;
	}
}