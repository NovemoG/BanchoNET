using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Players;
using BanchoNET.Packets;
using BanchoNET.Utils;

namespace BanchoNET.Services;

public partial class BanchoHandler
{
	private async Task CreateMatch(Player player, BinaryReader br)
	{
		if (player.Restricted)
		{
			using var joinFailPacket = new ServerPackets();
			joinFailPacket.MatchJoinFail();
			joinFailPacket.Notification("Multiplayer is not available while restricted.");
			player.Enqueue(joinFailPacket.GetContent());
			return;
		}

		if (player.Silenced)
		{
			using var joinFailPacket = new ServerPackets();
			joinFailPacket.MatchJoinFail();
			joinFailPacket.Notification("Multiplayer is not available while silenced.");
			player.Enqueue(joinFailPacket.GetContent());
			return;
		}
		
		var matchData = br.ReadOsuMatch();
		var matchChannel = new Channel
		{
			Name = $"multi_{matchData.Id}",
			Description = $"MID {matchData.Id}'s multiplayer channel.",
			AutoJoin = false,
			Instance = true
		};

		matchData.LobbyId = await GetMatchId();
		matchData.Chat = matchChannel; 
		
		_session.InsertLobby(matchData);
		_session.InsertChannel(matchChannel);

		player.JoinMatch(matchData, matchData.Password);
		player.LastActivityTime = DateTime.Now;
		
		matchChannel.SendBotMessage($"Match created by {player.Username} {matchData.MPLinkEmbed()}");
		Console.WriteLine($"[CreateMatch] {player.Username} created a match with ID {matchData.LobbyId}, in-game ID: {matchData.Id}.");
	}
}