using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Multiplayer;
using BanchoNET.Objects.Players;
using BanchoNET.Services;
using BanchoNET.Utils;
using BanchoNET.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Packets;

public sealed partial class ServerPacket : IDisposable
{
	private readonly MemoryStream _dataBuffer;
	private readonly BinaryWriter _binaryWriter;

	private bool _disposed;
	
	public ServerPacket()
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
	
	public byte[] FinalizeAndGetContent()
	{
		var data = _dataBuffer.ToArray();
		Dispose();
		return data;
	}

	public FileContentResult FinalizeAndGetContentResult()
	{
		var data = _dataBuffer.ToArray();
		Dispose();
		return Responses.BytesContentResult(data);
	}
	
	public ServerPacket WriteBytes(byte[] data)
	{
		_binaryWriter.Write(data);
		return this;
	}
	
	/// <summary>
	/// Packet id 5
	/// </summary>
	public ServerPacket PlayerId(int playerId)
	{
		WritePacketData(ServerPacketId.UserId, new PacketData(playerId, DataType.Int));
		return this;
	}

	/// <summary>
	/// Packet id 7
	/// </summary>
	public ServerPacket SendMessage(Message message)
	{
		WritePacketData(ServerPacketId.SendMessage, new PacketData(message, DataType.Message));
		return this;
	}

	/// <summary>
	/// Packet id 8
	/// </summary>
	public ServerPacket Pong()
	{
		WritePacketData(ServerPacketId.Pong);
		return this;
	}
	
	/// <summary>
	/// Packet id 9
	/// </summary>
	[Obsolete("No longer used by osu! client.")]
	public ServerPacket ChangeUsername(string oldName, string newName)
	{
		WritePacketData(ServerPacketId.HandleIrcChangeUsername, new PacketData($"{oldName}>>>>{newName}", DataType.String));
		return this;
	}
	
	/// <summary>
	/// Packet id 11
	/// </summary>
	public ServerPacket UserStats(Player player)
	{
		WritePacketData(ServerPacketId.UserStats, new PacketData(player, DataType.Stats));
		return this;
	}
	
	/// <summary>
	/// Packet id 11
	/// </summary>
	public ServerPacket BotStats(Player player)
	{
		WritePacketData(ServerPacketId.UserStats, new PacketData(player, DataType.BotStats));
		return this;
	}
	
	/// <summary>
	/// Packet id 12
	/// </summary>
	public ServerPacket Logout(int playerId)
	{
		WritePacketData(ServerPacketId.UserLogout, new PacketData(playerId, DataType.Int), new PacketData((byte)0, DataType.Byte));
		return this;
	}

	/// <summary>
	/// Packet id 13
	/// </summary>
	public ServerPacket SpectatorJoined(int playerId)
	{
		WritePacketData(ServerPacketId.SpectatorJoined, new PacketData(playerId, DataType.Int));
		return this;
	}

	/// <summary>
	/// Packet id 42
	/// </summary>
	public ServerPacket FellowSpectatorJoined(int playerId)
	{
		WritePacketData(ServerPacketId.FellowSpectatorJoined, new PacketData(playerId, DataType.Int));
		return this;
	}

	/// <summary>
	/// Packet id 14
	/// </summary>
	public ServerPacket SpectatorLeft(int playerId)
	{
		WritePacketData(ServerPacketId.SpectatorLeft, new PacketData(playerId, DataType.Int));
		return this;
	}
	
	/// <summary>
	/// Packet id 43
	/// </summary>
	public ServerPacket FellowSpectatorLeft(int playerId)
	{
		WritePacketData(ServerPacketId.FellowSpectatorLeft, new PacketData(playerId, DataType.Int));
		return this;
	}

	/// <summary>
	/// Packet id 15
	/// </summary>
	public ServerPacket SpectateFrames(byte[] rawData)
	{
		WritePacketData(ServerPacketId.SpectateFrames, new PacketData(rawData, DataType.Raw));
		return this;
	}

	/// <summary>
	/// Packet id 22
	/// </summary>
	public ServerPacket SpectatorCantSpectate(int playerId)
	{
		WritePacketData(ServerPacketId.SpectatorCantSpectate, new PacketData(playerId, DataType.Int));
		return this;
	}
	
	/// <summary>
	/// Packet id 19
	/// </summary>
	public ServerPacket VersionUpdate()
	{
		WritePacketData(ServerPacketId.VersionUpdate);
		return this;
	}

	/// <summary>
	/// Packet id 23
	/// </summary>
	public ServerPacket GetAttention()
	{
		WritePacketData(ServerPacketId.GetAttention);
		return this;
	}
	
	/// <summary>
	/// Packet id 24
	/// </summary>
	public ServerPacket Notification(string message)
	{
		WritePacketData(ServerPacketId.Notification, new PacketData(message, DataType.String));
		return this;
	}

	/// <summary>
	/// Packet id 26
	/// </summary>
	public ServerPacket UpdateMatch(MultiplayerLobby lobby, bool sendPassword)
	{
		var data = new LobbyData
		{
			Lobby = lobby,
			SendPassword = sendPassword
		};
		WritePacketData(ServerPacketId.UpdateMatch, new PacketData(data, DataType.Match));
		return this;
	}
	
	/// <summary>
	/// Packet id 27
	/// </summary>
	public ServerPacket NewMatch(MultiplayerLobby lobby)
	{
		var data = new LobbyData
		{
			Lobby = lobby,
			SendPassword = true
		};
		WritePacketData(ServerPacketId.NewMatch, new PacketData(data, DataType.Match));
		return this;
	}

	/// <summary>
	/// Packet id 28
	/// </summary>
	public ServerPacket DisposeMatch(MultiplayerLobby lobby)
	{
		WritePacketData(ServerPacketId.DisposeMatch, new PacketData((int)lobby.Id, DataType.Int));
		return this;
	}

	/// <summary>
	/// Packet id 36
	/// </summary>
	public ServerPacket MatchJoinSuccess(MultiplayerLobby lobby)
	{
		var data = new LobbyData
		{
			Lobby = lobby,
			SendPassword = true
		};
		WritePacketData(ServerPacketId.MatchJoinSuccess, new PacketData(data, DataType.Match));
		return this;
	}

	/// <summary>
	/// Packet id 37
	/// </summary>
	public ServerPacket MatchJoinFail()
	{
		WritePacketData(ServerPacketId.MatchJoinFail);
		return this;
	}
	
	/// <summary>
	/// Packet id 46
	/// </summary>
	public ServerPacket MatchStart(MultiplayerLobby lobby)
	{
		var data = new LobbyData
		{
			Lobby = lobby,
			SendPassword = true
		};
		WritePacketData(ServerPacketId.MatchStart, new PacketData(data, DataType.Match));
		return this;
	}

	/// <summary>
	/// Packet id 48
	/// </summary>
	public ServerPacket MatchScoreUpdate(byte[] rawData)
	{
		WritePacketData(ServerPacketId.MatchScoreUpdate, new PacketData(rawData, DataType.Raw));
		return this;
	}

	/// <summary>
	/// Packet id 50
	/// </summary>
	public ServerPacket MatchTransferHost()
	{
		WritePacketData(ServerPacketId.MatchTransferHost);
		return this;
	}
	
	/// <summary>
	/// Packet id 53
	/// </summary>
	public ServerPacket MatchAllPlayersLoaded()
	{
		WritePacketData(ServerPacketId.MatchAllPlayersLoaded);
		return this;
	}
	
	/// <summary>
	/// Packet id 57
	/// </summary>
	public ServerPacket MatchPlayerFailed(int slotId)
	{
		WritePacketData(ServerPacketId.MatchPlayerFailed, new PacketData(slotId, DataType.Int));
		return this;
	}

	/// <summary>
	/// Packet id 58
	/// </summary>
	public ServerPacket MatchComplete()
	{
		WritePacketData(ServerPacketId.MatchComplete);
		return this;
	}

	/// <summary>
	/// Packet id 61
	/// </summary>
	public ServerPacket MatchSkip()
	{
		WritePacketData(ServerPacketId.MatchSkip);
		return this;
	}
	
	/// <summary>
	/// Packet id 81
	/// </summary>
	public ServerPacket MatchPlayerSkipped(int playerId)
	{
		WritePacketData(ServerPacketId.MatchPlayerSkipped, new PacketData(playerId, DataType.Int));
		return this;
	}

	/// <summary>
	/// Packet id 88
	/// </summary>
	public ServerPacket MatchInvite(Player player, string targetName)
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
		return this;
	}

	/// <summary>
	/// Packet id 91
	/// </summary>
	[Obsolete("Currently unused")]
	public ServerPacket MatchChangePassword(string newPassword)
	{
		WritePacketData(ServerPacketId.MatchChangePassword, new PacketData(newPassword, DataType.String));
		return this;
	}

	/// <summary>
	/// Packet id 106
	/// </summary>
	public ServerPacket MatchAbort()
	{
		WritePacketData(ServerPacketId.MatchAbort);
		return this;
	}

	/// <summary>
	/// Packet id 34
	/// </summary>
	public ServerPacket ToggleBlockNonFriendDm()
	{
		WritePacketData(ServerPacketId.ToggleBlockNonFriendDms);
		return this;
	}

	/// <summary>
	/// Packet id 64
	/// </summary>
	public ServerPacket ChannelJoin(string name)
	{
		WritePacketData(ServerPacketId.ChannelJoinSuccess, new PacketData(name, DataType.String));
		return this;
	}

	/// <summary>
	/// Packet id 65
	/// </summary>
	public ServerPacket ChannelInfo(Channel channel)
	{
		WritePacketData(ServerPacketId.ChannelInfo, new PacketData(channel, DataType.Channel));
		return this;
	}
	
	/// <summary>
	/// Packet id 89
	/// </summary>
	public ServerPacket ChannelInfoEnd()
	{
		WritePacketData(ServerPacketId.ChannelInfoEnd);
		return this;
	}

	/// <summary>
	/// Packet id 66
	/// </summary>
	public ServerPacket ChannelKick(string name)
	{
		WritePacketData(ServerPacketId.ChannelKick, new PacketData(name, DataType.String));
		return this;
	}

	/// <summary>
	/// Packet id 67
	/// </summary>
	public ServerPacket ChannelAutoJoin(Channel channel)
	{
		WritePacketData(ServerPacketId.ChannelAutoJoin, new PacketData(channel, DataType.Channel));
		return this;
	}

	/// <summary>
	/// Packet id 69
	/// </summary>
	[Obsolete]
	public ServerPacket BeatmapInfoReply()
	{
		return this;
	}
	
	/// <summary>
	/// Packet id 71
	/// </summary>
	public ServerPacket BanchoPrivileges(int privileges)
	{
		WritePacketData(ServerPacketId.Privileges, new PacketData(privileges, DataType.Int));
		return this;
	}

	/// <summary>
	/// Packet id 72
	/// </summary>
	public ServerPacket FriendsList(List<int> friends)
	{
		WritePacketData(ServerPacketId.FriendsList, new PacketData(friends, DataType.IntList));
		return this;
	}
	
	/// <summary>
	/// Packet id 75
	/// </summary>
	public ServerPacket ProtocolVersion(int version)
	{
		WritePacketData(ServerPacketId.ProtocolVersion, new PacketData(version, DataType.Int));
		return this;
	}
	
	/// <summary>
	/// Packet id 76
	/// </summary>
	public ServerPacket MainMenuIcon(string iconUrl, string onclickUrl)
	{
		WritePacketData(ServerPacketId.MainMenuIcon, new PacketData($"{iconUrl}|{onclickUrl}", DataType.String));
		return this;
	}

	/// <summary>
	/// Packet id 80
	/// </summary>
	[Obsolete("No longer used by osu! client.")]
	public ServerPacket Monitor()
	{
		WritePacketData(ServerPacketId.Monitor);
		return this;
	}

	/// <summary>
	/// Packet id 83
	/// </summary>
	public ServerPacket BotPresence(Player player)
	{
		WritePacketData(ServerPacketId.UserPresence, new PacketData(player, DataType.BotPresence));
		return this;
	}
	
	/// <summary>
	/// Packet id 83
	/// </summary>
	public ServerPacket UserPresence(Player player)
	{
		WritePacketData(ServerPacketId.UserPresence, new PacketData(player, DataType.Presence));
		return this;
	}
	
	/// <summary>
	/// Packet id 86
	/// </summary>
	public ServerPacket RestartServer(int msToReconnect)
	{
		WritePacketData(ServerPacketId.Restart, new PacketData(msToReconnect, DataType.Int));
		return this;
	}
	
	/// <summary>
	/// Packet id 92
	/// </summary>
	public ServerPacket SilenceEnd(int delta)
	{
		WritePacketData(ServerPacketId.SilenceEnd, new PacketData(delta, DataType.Int));
		return this;
	}

	/// <summary>
	/// Packet id 94
	/// </summary>
	public ServerPacket UserSilenced(int playerId)
	{
		WritePacketData(ServerPacketId.UserSilenced, new PacketData(playerId, DataType.Int));
		return this;
	}

	/// <summary>
	/// Packet id 95
	/// </summary>
	[Obsolete("No longer used by osu! client.")]
	public ServerPacket UserPresenceSingle(int playerId)
	{
		WritePacketData(ServerPacketId.UserPresenceSingle, new PacketData(playerId, DataType.Int));
		return this;
	}
	
	/// <summary>
	/// Packet id 96
	/// </summary>
	[Obsolete("No longer used by osu! client.")]
	public ServerPacket UserPresenceBundle(List<int> playerIds)
	{
		WritePacketData(ServerPacketId.UserPresenceBundle, new PacketData(playerIds, DataType.IntList));
		return this;
	}

	/// <summary>
	/// Packet id 100
	/// </summary>
	public ServerPacket UserDmBlocked(string target)
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
		return this;
	}

	/// <summary>
	/// Packet id 101
	/// </summary>
	public ServerPacket TargetSilenced(string target)
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
		return this;
	}

	/// <summary>
	/// Packet id 102
	/// </summary>
	public ServerPacket VersionUpdateForced()
	{
		WritePacketData(ServerPacketId.VersionUpdateForced);
		return this;
	}

	/// <summary>
	/// Packet id 103
	/// </summary>
	public ServerPacket SwitchServer(int time)
	{
		WritePacketData(ServerPacketId.SwitchServer, new PacketData(time, DataType.String));
		return this;
	}

	/// <summary>
	/// Packet id 107
	/// </summary>
	public ServerPacket SwitchTournamentServer(string ip)
	{
		WritePacketData(ServerPacketId.SwitchTournamentServer, new PacketData(ip, DataType.String));
		return this;
	}
	
	/// <summary>
	/// Packet id 104
	/// </summary>
	public ServerPacket AccountRestricted()
	{
		WritePacketData(ServerPacketId.AccountRestricted);
		return this;
	}

	/// <summary>
	/// Packet id 105
	/// </summary>
	[Obsolete("Shouldn't be sent to osu! client.")]
	public ServerPacket RTX(string message)
	{
		WritePacketData(ServerPacketId.Rtx, new PacketData(message, DataType.String));
		return this;
	}

	/// <summary>
	/// Enqueues data of other players to player's buffer and if specified it also provides player's data to other players
	/// </summary>
	/// <param name="player">Player from which data will be enqueued to others</param>
	public ServerPacket OtherPlayers(Player? player = null)
	{
		var session = BanchoSession.Instance;
		var toOthers = player != null;
		
		using var playerLogin = new ServerPacket();
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

		if (!toOthers) return this;
		
		foreach (var restricted in session.Restricted)
			restricted.Enqueue(loginData);

		return this;
	}

	public void Dispose()
	{
		Dispose(true);
	}

	private void Dispose(bool disposing)
	{
		if (_disposed) return;
		
		if (disposing)
		{
			_dataBuffer.Dispose();
			_binaryWriter.Dispose();
		}
		
		_disposed = true;
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