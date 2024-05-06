using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Multiplayer;
using BanchoNET.Objects.Players;
using BanchoNET.Services;
using BanchoNET.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Packets;

public partial class ServerPackets : IDisposable
{
	private readonly MemoryStream _dataBuffer;
	private readonly BinaryWriter _binaryWriter;
	
	public ServerPackets()
	{
		_dataBuffer = new MemoryStream();
		_binaryWriter = new BinaryWriter(_dataBuffer);
	}
	
	public byte[] GetContent()
	{
		return _dataBuffer.ToArray();
	}

	public FileContentResult GetContentResult()
	{
		return Responses.BytesContentResult(_dataBuffer.ToArray());
	}
	
	public void WriteBytes(byte[] data)
	{
		_binaryWriter.Write(data);
	}
	
	/// <summary>
	/// Packet id 5
	/// </summary>
	public void PlayerId(int playerId)
	{
		WritePacketData(ServerPacketId.UserId, new PacketData(playerId, DataType.Int));
	}

	/// <summary>
	/// Packet id 7
	/// </summary>
	public void SendMessage(Message message)
	{
		WritePacketData(ServerPacketId.SendMessage, new PacketData(message, DataType.Message));
	}

	/// <summary>
	/// Packet id 8
	/// </summary>
	public void Pong()
	{
		WritePacketData(ServerPacketId.Pong);
	}
	
	/// <summary>
	/// Packet id 9
	/// </summary>
	[Obsolete("No longer used by osu! client.")]
	public void ChangeUsername(string oldName, string newName)
	{
		WritePacketData(ServerPacketId.HandleIrcChangeUsername, new PacketData($"{oldName}>>>>{newName}", DataType.String));
	}
	
	/// <summary>
	/// Packet id 11
	/// </summary>
	public void UserStats(Player player)
	{
		WritePacketData(ServerPacketId.UserStats, new PacketData(player, DataType.Stats));
	}
	
	/// <summary>
	/// Packet id 11
	/// </summary>
	public void BotStats(Player player)
	{
		WritePacketData(ServerPacketId.UserStats, new PacketData(player, DataType.BotStats));
	}
	
	/// <summary>
	/// Packet id 12
	/// </summary>
	public void Logout(int playerId)
	{
		WritePacketData(ServerPacketId.UserLogout, new PacketData(playerId, DataType.Int), new PacketData((byte)0, DataType.Byte));
	}

	/// <summary>
	/// Packet id 13
	/// </summary>
	public void SpectatorJoined(int playerId)
	{
		WritePacketData(ServerPacketId.SpectatorJoined, new PacketData(playerId, DataType.Int));
	}

	/// <summary>
	/// Packet id 42
	/// </summary>
	public void FellowSpectatorJoined(int playerId)
	{
		WritePacketData(ServerPacketId.FellowSpectatorJoined, new PacketData(playerId, DataType.Int));
	}

	/// <summary>
	/// Packet id 14
	/// </summary>
	public void SpectatorLeft(int playerId)
	{
		WritePacketData(ServerPacketId.SpectatorLeft, new PacketData(playerId, DataType.Int));
	}
	
	/// <summary>
	/// Packet id 43
	/// </summary>
	public void FellowSpectatorLeft(int playerId)
	{
		WritePacketData(ServerPacketId.FellowSpectatorLeft, new PacketData(playerId, DataType.Int));
	}

	/// <summary>
	/// Packet id 15
	/// </summary>
	public void SpectateFrames(byte[] rawData)
	{
		WritePacketData(ServerPacketId.SpectateFrames, new PacketData(rawData, DataType.Raw));
	}

	/// <summary>
	/// Packet id 22
	/// </summary>
	public void SpectatorCantSpectate(int playerId)
	{
		WritePacketData(ServerPacketId.SpectatorCantSpectate, new PacketData(playerId, DataType.Int));
	}
	
	/// <summary>
	/// Packet id 19
	/// </summary>
	public void VersionUpdate()
	{
		WritePacketData(ServerPacketId.VersionUpdate);
	}

	/// <summary>
	/// Packet id 23
	/// </summary>
	public void GetAttention()
	{
		WritePacketData(ServerPacketId.GetAttention);
	}
	
	/// <summary>
	/// Packet id 24
	/// </summary>
	public void Notification(string message)
	{
		WritePacketData(ServerPacketId.Notification, new PacketData(message, DataType.String));
	}

	/// <summary>
	/// Packet id 26
	/// </summary>
	public void UpdateMatch(MultiplayerLobby lobby, bool sendPassword)
	{
		var data = new LobbyData
		{
			Lobby = lobby,
			SendPassword = sendPassword
		};
		WritePacketData(ServerPacketId.UpdateMatch, new PacketData(data, DataType.Match));
	}
	
	/// <summary>
	/// Packet id 27
	/// </summary>
	public void NewMatch(MultiplayerLobby lobby)
	{
		var data = new LobbyData
		{
			Lobby = lobby,
			SendPassword = true
		};
		WritePacketData(ServerPacketId.NewMatch, new PacketData(data, DataType.Match));
	}

	/// <summary>
	/// Packet id 28
	/// </summary>
	public void DisposeMatch(MultiplayerLobby lobby)
	{
		WritePacketData(ServerPacketId.DisposeMatch, new PacketData((int)lobby.Id, DataType.Int));
	}

	/// <summary>
	/// Packet id 36
	/// </summary>
	public void MatchJoinSuccess(MultiplayerLobby lobby)
	{
		var data = new LobbyData
		{
			Lobby = lobby,
			SendPassword = true
		};
		WritePacketData(ServerPacketId.MatchJoinSuccess, new PacketData(data, DataType.Match));
	}

	/// <summary>
	/// Packet id 37
	/// </summary>
	public void MatchJoinFail()
	{
		WritePacketData(ServerPacketId.MatchJoinFail);
	}
	
	/// <summary>
	/// Packet id 46
	/// </summary>
	public void MatchStart(MultiplayerLobby lobby)
	{
		var data = new LobbyData
		{
			Lobby = lobby,
			SendPassword = true
		};
		WritePacketData(ServerPacketId.MatchStart, new PacketData(data, DataType.Match));
	}

	/// <summary>
	/// Packet id 48
	/// </summary>
	public void MatchScoreUpdate(byte[] rawData)
	{
		WritePacketData(ServerPacketId.MatchScoreUpdate, new PacketData(rawData, DataType.Raw));
	}

	/// <summary>
	/// Packet id 50
	/// </summary>
	public void MatchTransferHost()
	{
		WritePacketData(ServerPacketId.MatchTransferHost);
	}
	
	/// <summary>
	/// Packet id 53
	/// </summary>
	public void MatchAllPlayersLoaded()
	{
		WritePacketData(ServerPacketId.MatchAllPlayersLoaded);
	}
	
	/// <summary>
	/// Packet id 57
	/// </summary>
	public void MatchPlayerFailed(int slotId)
	{
		WritePacketData(ServerPacketId.MatchPlayerFailed, new PacketData(slotId, DataType.Int));
	}

	/// <summary>
	/// Packet id 58
	/// </summary>
	public void MatchComplete()
	{
		WritePacketData(ServerPacketId.MatchComplete);
	}

	/// <summary>
	/// Packet id 61
	/// </summary>
	public void MatchSkip()
	{
		WritePacketData(ServerPacketId.MatchSkip);
	}
	
	/// <summary>
	/// Packet id 81
	/// </summary>
	public void MatchPlayerSkipped(int playerId)
	{
		WritePacketData(ServerPacketId.MatchPlayerSkipped, new PacketData(playerId, DataType.Int));
	}

	/// <summary>
	/// Packet id 88
	/// </summary>
	public void MatchInvite(Player player, string targetName)
	{
		WritePacketData(ServerPacketId.MatchInvite , new PacketData(
			new Message
			{
				Sender = player.Username,
				Content = $"Come join my game: {player.Lobby!.Embed()}",
				Destination = targetName,
				SenderId = player.Id
			},
			DataType.Message));
	}

	/// <summary>
	/// Packet id 91
	/// </summary>
	[Obsolete("Currently unused")]
	public void MatchChangePassword(string newPassword)
	{
		WritePacketData(ServerPacketId.MatchChangePassword, new PacketData(newPassword, DataType.String));
	}

	/// <summary>
	/// Packet id 106
	/// </summary>
	public void MatchAbort()
	{
		WritePacketData(ServerPacketId.MatchAbort);
	}

	/// <summary>
	/// Packet id 34
	/// </summary>
	public void ToggleBlockNonFriendDm()
	{
		WritePacketData(ServerPacketId.ToggleBlockNonFriendDms);
	}

	/// <summary>
	/// Packet id 64
	/// </summary>
	public void ChannelJoin(string name)
	{
		WritePacketData(ServerPacketId.ChannelJoinSuccess, new PacketData(name, DataType.String));
	}

	/// <summary>
	/// Packet id 65
	/// </summary>
	public void ChannelInfo(Channel channel)
	{
		WritePacketData(ServerPacketId.ChannelInfo, new PacketData(channel, DataType.Channel));
	}
	
	/// <summary>
	/// Packet id 89
	/// </summary>
	public void ChannelInfoEnd()
	{
		WritePacketData(ServerPacketId.ChannelInfoEnd);
	}

	/// <summary>
	/// Packet id 66
	/// </summary>
	public void ChannelKick(string name)
	{
		WritePacketData(ServerPacketId.ChannelKick, new PacketData(name, DataType.String));
	}

	/// <summary>
	/// Packet id 67
	/// </summary>
	public void ChannelAutoJoin(Channel channel)
	{
		WritePacketData(ServerPacketId.ChannelAutoJoin, new PacketData(channel, DataType.Channel));
	}

	/// <summary>
	/// Packet id 69
	/// </summary>
	[Obsolete]
	public void BeatmapInfoReply()
	{
		
	}
	
	/// <summary>
	/// Packet id 71
	/// </summary>
	public void BanchoPrivileges(int privileges)
	{
		WritePacketData(ServerPacketId.Privileges, new PacketData(privileges, DataType.Int));
	}

	/// <summary>
	/// Packet id 72
	/// </summary>
	public void FriendsList(List<int> friends)
	{
		WritePacketData(ServerPacketId.FriendsList, new PacketData(friends, DataType.IntList));
	}
	
	/// <summary>
	/// Packet id 75
	/// </summary>
	public void ProtocolVersion(int version)
	{
		WritePacketData(ServerPacketId.ProtocolVersion, new PacketData(version, DataType.Int));
	}
	
	/// <summary>
	/// Packet id 76
	/// </summary>
	public void MainMenuIcon(string iconUrl, string onclickUrl)
	{
		WritePacketData(ServerPacketId.MainMenuIcon, new PacketData($"{iconUrl}|{onclickUrl}", DataType.String));
	}

	/// <summary>
	/// Packet id 80
	/// </summary>
	[Obsolete("No longer used by osu! client.")]
	public void Monitor()
	{
		WritePacketData(ServerPacketId.Monitor);
	}

	/// <summary>
	/// Packet id 83
	/// </summary>
	public void BotPresence(Player player)
	{
		WritePacketData(ServerPacketId.UserPresence, new PacketData(player, DataType.BotPresence));
	}
	
	/// <summary>
	/// Packet id 83
	/// </summary>
	public void UserPresence(Player player)
	{
		WritePacketData(ServerPacketId.UserPresence, new PacketData(player, DataType.Presence));
	}
	
	/// <summary>
	/// Packet id 86
	/// </summary>
	public void RestartServer(int msToReconnect)
	{
		WritePacketData(ServerPacketId.Restart, new PacketData(msToReconnect, DataType.Int));
	}
	
	/// <summary>
	/// Packet id 92
	/// </summary>
	public void SilenceEnd(int delta)
	{
		WritePacketData(ServerPacketId.SilenceEnd, new PacketData(delta, DataType.Int));
	}

	/// <summary>
	/// Packet id 94
	/// </summary>
	public void UserSilenced(int playerId)
	{
		WritePacketData(ServerPacketId.UserSilenced, new PacketData(playerId, DataType.Int));
	}

	/// <summary>
	/// Packet id 95
	/// </summary>
	[Obsolete("No longer used by osu! client.")]
	public void UserPresenceSingle(int playerId)
	{
		WritePacketData(ServerPacketId.UserPresenceSingle, new PacketData(playerId, DataType.Int));
	}
	
	/// <summary>
	/// Packet id 96
	/// </summary>
	[Obsolete("No longer used by osu! client.")]
	public void UserPresenceBundle(List<int> playerIds)
	{
		WritePacketData(ServerPacketId.UserPresenceBundle, new PacketData(playerIds, DataType.IntList));
	}

	/// <summary>
	/// Packet id 100
	/// </summary>
	public void UserDmBlocked(string target)
	{
		WritePacketData(ServerPacketId.UserDmBlocked,
			new PacketData(
				new Message
				{
					Sender = "", 
					Content = "", 
					Destination = target, 
					SenderId = 0
				},
				DataType.Message));
	}

	/// <summary>
	/// Packet id 101
	/// </summary>
	public void TargetSilenced(string target)
	{
		WritePacketData(ServerPacketId.TargetIsSilenced,
			new PacketData(
				new Message
				{
					Sender = "", 
					Content = "", 
					Destination = target, 
					SenderId = 0
				},
				DataType.Message));
	}

	/// <summary>
	/// Packet id 102
	/// </summary>
	public void VersionUpdateForced()
	{
		WritePacketData(ServerPacketId.VersionUpdateForced);
	}

	/// <summary>
	/// Packet id 103
	/// </summary>
	public void SwitchServer(int time)
	{
		WritePacketData(ServerPacketId.SwitchServer, new PacketData(time, DataType.String));
	}

	/// <summary>
	/// Packet id 107
	/// </summary>
	public void SwitchTournamentServer(string ip)
	{
		WritePacketData(ServerPacketId.SwitchTournamentServer, new PacketData(ip, DataType.String));
	}
	
	/// <summary>
	/// Packet id 104
	/// </summary>
	public void AccountRestricted()
	{
		WritePacketData(ServerPacketId.AccountRestricted);
	}

	/// <summary>
	/// Packet id 105
	/// </summary>
	[Obsolete("Shouldn't be sent to osu! client.")]
	public void RTX(string message)
	{
		WritePacketData(ServerPacketId.Rtx, new PacketData(message, DataType.String));
	}

	/// <summary>
	/// Enqueues data of other players to player's buffer and if specified it also provides player's data to other players
	/// </summary>
	/// <param name="player">Player from which data will be enqueued to others</param>
	public void OtherPlayers(Player? player = null)
	{
		var session = BanchoSession.Instance;
		var toOthers = player != null;
		
		using var playerLogin = new ServerPackets();
		if (toOthers)
		{
			playerLogin.UserPresence(player!);
			playerLogin.UserStats(player!);
		}
		var loginData = playerLogin.GetContent();
		
		foreach (var bot in session.Bots)
		{
			BotPresence(bot);
			BotStats(bot);
		}
		
		foreach (var user in session.Players)
		{
			if (toOthers) user.Enqueue(loginData);
			UserPresence(user);
			UserStats(user);
		}

		if (!toOthers) return;
		
		foreach (var restricted in session.Restricted)
			restricted.Enqueue(loginData);
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (!disposing) return;
		
		_dataBuffer.Dispose();
		_binaryWriter.Dispose();
	}

	private struct PacketData(object? data, DataType type)
	{
		public readonly object? Data = data;
		public readonly DataType Type = type;
	}

	private enum DataType
	{
		SByte,
		Byte,
		Short,
		UShort,
		Int,
		UInt,
		Long,
		ULong,
		Float,
		Double,
		Message,
		Channel,
		Match,
		Stats,
		BotStats,
		Presence,
		BotPresence,
		ScoreFrame,
		MapInfoRequest,
		MapInfoReply,
		ReplayFrameBundle,
		IntList,
		String,
		Raw
	}
}