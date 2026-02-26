using BanchoNET.Core.Models.Channels;
using BanchoNET.Core.Models.Multiplayer;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Users;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BanchoNET.Core.Packets;

public sealed partial class ServerPackets : IDisposable
{
	private readonly MemoryStream _dataBuffer;
	private readonly BinaryWriter _binaryWriter;

	private bool _disposed;
	
	public ServerPackets()
	{
		_dataBuffer = new MemoryStream();
		_binaryWriter = new BinaryWriter(_dataBuffer);
	}

	public void Clear()
	{
		_dataBuffer.Position = 0;
		_dataBuffer.SetLength(0);

		_binaryWriter.BaseStream.Position = 0;
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
	
	public ServerPackets WriteBytes(byte[] data)
	{
		_binaryWriter.Write(data);
		return this;
	}
	
	/// <summary>
	/// Packet id 5
	/// </summary>
	public ServerPackets PlayerId(int playerId)
	{
		WritePacketData(ServerPacketId.UserId, new PacketData(playerId, DataType.Int));
		return this;
	}

	/// <summary>
	/// Packet id 7
	/// </summary>
	public ServerPackets SendMessage(Message message)
	{
		WritePacketData(ServerPacketId.SendMessage, new PacketData(message, DataType.Message));
		return this;
	}

	/// <summary>
	/// Packet id 8
	/// </summary>
	public ServerPackets Pong()
	{
		WritePacketData(ServerPacketId.Pong);
		return this;
	}
	
	/// <summary>
	/// Packet id 9
	/// </summary>
	[Obsolete("No longer used by osu! client.")]
	public ServerPackets ChangeUsername(string oldName, string newName)
	{
		WritePacketData(ServerPacketId.HandleIrcChangeUsername, new PacketData($"{oldName}>>>>{newName}", DataType.String));
		return this;
	}
	
	/// <summary>
	/// Packet id 11
	/// </summary>
	public ServerPackets UserStats(User player)
	{
		WritePacketData(ServerPacketId.UserStats, new PacketData(player, DataType.Stats));
		return this;
	}
	
	/// <summary>
	/// Packet id 11
	/// </summary>
	public ServerPackets BotStats(User player)
	{
		WritePacketData(ServerPacketId.UserStats, new PacketData(player, DataType.BotStats));
		return this;
	}
	
	/// <summary>
	/// Packet id 12
	/// </summary>
	public ServerPackets Logout(int playerId)
	{
		WritePacketData(ServerPacketId.UserLogout, new PacketData(playerId, DataType.Int), new PacketData((byte)0, DataType.Byte));
		return this;
	}

	/// <summary>
	/// Packet id 13
	/// </summary>
	public ServerPackets SpectatorJoined(int playerId)
	{
		WritePacketData(ServerPacketId.SpectatorJoined, new PacketData(playerId, DataType.Int));
		return this;
	}

	/// <summary>
	/// Packet id 42
	/// </summary>
	public ServerPackets FellowSpectatorJoined(int playerId)
	{
		WritePacketData(ServerPacketId.FellowSpectatorJoined, new PacketData(playerId, DataType.Int));
		return this;
	}

	/// <summary>
	/// Packet id 14
	/// </summary>
	public ServerPackets SpectatorLeft(int playerId)
	{
		WritePacketData(ServerPacketId.SpectatorLeft, new PacketData(playerId, DataType.Int));
		return this;
	}
	
	/// <summary>
	/// Packet id 43
	/// </summary>
	public ServerPackets FellowSpectatorLeft(int playerId)
	{
		WritePacketData(ServerPacketId.FellowSpectatorLeft, new PacketData(playerId, DataType.Int));
		return this;
	}

	/// <summary>
	/// Packet id 15
	/// </summary>
	public ServerPackets SpectateFrames(byte[] rawData)
	{
		WritePacketData(ServerPacketId.SpectateFrames, new PacketData(rawData, DataType.Raw));
		return this;
	}

	/// <summary>
	/// Packet id 22
	/// </summary>
	public ServerPackets SpectatorCantSpectate(int playerId)
	{
		WritePacketData(ServerPacketId.SpectatorCantSpectate, new PacketData(playerId, DataType.Int));
		return this;
	}
	
	/// <summary>
	/// Packet id 19
	/// </summary>
	public ServerPackets VersionUpdate()
	{
		WritePacketData(ServerPacketId.VersionUpdate);
		return this;
	}

	/// <summary>
	/// Packet id 23
	/// </summary>
	public ServerPackets GetAttention()
	{
		WritePacketData(ServerPacketId.GetAttention);
		return this;
	}
	
	/// <summary>
	/// Packet id 24
	/// </summary>
	public ServerPackets Notification(string message)
	{
		WritePacketData(ServerPacketId.Notification, new PacketData(message, DataType.String));
		return this;
	}

	/// <summary>
	/// Packet id 26
	/// </summary>
	public ServerPackets UpdateMatch(MultiplayerMatch match, bool sendPassword)
	{
		WritePacketData(ServerPacketId.UpdateMatch, 
			new PacketData(new LobbyData(match, sendPassword), DataType.Match));
		return this;
	}
	
	/// <summary>
	/// Packet id 27
	/// </summary>
	public ServerPackets NewMatch(MultiplayerMatch match)
	{
		WritePacketData(ServerPacketId.NewMatch,
			new PacketData(new LobbyData(match, true), DataType.Match));
		return this;
	}

	/// <summary>
	/// Packet id 28
	/// </summary>
	public ServerPackets DisposeMatch(MultiplayerMatch match)
	{
		WritePacketData(ServerPacketId.DisposeMatch, new PacketData((int)match.Id, DataType.Int));
		return this;
	}

	/// <summary>
	/// Packet id 36
	/// </summary>
	public ServerPackets MatchJoinSuccess(MultiplayerMatch match)
	{
		WritePacketData(ServerPacketId.MatchJoinSuccess,
			new PacketData(new LobbyData(match, true), DataType.Match));
		return this;
	}

	/// <summary>
	/// Packet id 37
	/// </summary>
	public ServerPackets MatchJoinFail()
	{
		WritePacketData(ServerPacketId.MatchJoinFail);
		return this;
	}
	
	/// <summary>
	/// Packet id 46
	/// </summary>
	public ServerPackets MatchStart(MultiplayerMatch match)
	{
		WritePacketData(ServerPacketId.MatchStart,
			new PacketData(new LobbyData(match, true), DataType.Match));
		return this;
	}

	/// <summary>
	/// Packet id 48
	/// </summary>
	public ServerPackets MatchScoreUpdate(byte[] rawData)
	{
		WritePacketData(ServerPacketId.MatchScoreUpdate, new PacketData(rawData, DataType.Raw));
		return this;
	}

	/// <summary>
	/// Packet id 50
	/// </summary>
	public ServerPackets MatchTransferHost()
	{
		WritePacketData(ServerPacketId.MatchTransferHost);
		return this;
	}
	
	/// <summary>
	/// Packet id 53
	/// </summary>
	public ServerPackets MatchAllPlayersLoaded()
	{
		WritePacketData(ServerPacketId.MatchAllPlayersLoaded);
		return this;
	}
	
	/// <summary>
	/// Packet id 57
	/// </summary>
	public ServerPackets MatchPlayerFailed(int slotId)
	{
		WritePacketData(ServerPacketId.MatchPlayerFailed, new PacketData(slotId, DataType.Int));
		return this;
	}

	/// <summary>
	/// Packet id 58
	/// </summary>
	public ServerPackets MatchComplete()
	{
		WritePacketData(ServerPacketId.MatchComplete);
		return this;
	}

	/// <summary>
	/// Packet id 61
	/// </summary>
	public ServerPackets MatchSkip()
	{
		WritePacketData(ServerPacketId.MatchSkip);
		return this;
	}
	
	/// <summary>
	/// Packet id 81
	/// </summary>
	public ServerPackets MatchPlayerSkipped(int playerId)
	{
		WritePacketData(ServerPacketId.MatchPlayerSkipped, new PacketData(playerId, DataType.Int));
		return this;
	}

	/// <summary>
	/// Packet id 88
	/// </summary>
	public ServerPackets MatchInvite(User player, string targetName)
	{
		WritePacketData(ServerPacketId.MatchInvite , new PacketData(
			new Message
			{
				Sender = player.Username,
				Content = $"Come join my game: {player.Match!.Embed()}",
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
	public ServerPackets MatchChangePassword(string newPassword)
	{
		WritePacketData(ServerPacketId.MatchChangePassword, new PacketData(newPassword, DataType.String));
		return this;
	}

	/// <summary>
	/// Packet id 106
	/// </summary>
	public ServerPackets MatchAbort()
	{
		WritePacketData(ServerPacketId.MatchAbort);
		return this;
	}

	/// <summary>
	/// Packet id 34
	/// </summary>
	public ServerPackets ToggleBlockNonFriendDm()
	{
		WritePacketData(ServerPacketId.ToggleBlockNonFriendDms);
		return this;
	}

	/// <summary>
	/// Packet id 64
	/// </summary>
	public ServerPackets ChannelJoin(string name)
	{
		WritePacketData(ServerPacketId.ChannelJoinSuccess, new PacketData(name, DataType.String));
		return this;
	}

	/// <summary>
	/// Packet id 65
	/// </summary>
	public ServerPackets ChannelInfo(Channel channel)
	{
		WritePacketData(ServerPacketId.ChannelInfo, new PacketData(channel, DataType.Channel));
		return this;
	}
	
	/// <summary>
	/// Packet id 89
	/// </summary>
	public ServerPackets ChannelInfoEnd()
	{
		WritePacketData(ServerPacketId.ChannelInfoEnd);
		return this;
	}

	/// <summary>
	/// Packet id 66
	/// </summary>
	public ServerPackets ChannelKick(string name)
	{
		WritePacketData(ServerPacketId.ChannelKick, new PacketData(name, DataType.String));
		return this;
	}

	/// <summary>
	/// Packet id 67
	/// </summary>
	public ServerPackets ChannelAutoJoin(Channel channel)
	{
		WritePacketData(ServerPacketId.ChannelAutoJoin, new PacketData(channel, DataType.Channel));
		return this;
	}

	/// <summary>
	/// Packet id 69
	/// </summary>
	[Obsolete]
	public ServerPackets BeatmapInfoReply()
	{
		return this;
	}
	
	/// <summary>
	/// Packet id 71
	/// </summary>
	public ServerPackets BanchoPrivileges(int privileges)
	{
		WritePacketData(ServerPacketId.Privileges, new PacketData(privileges, DataType.Int));
		return this;
	}

	/// <summary>
	/// Packet id 72
	/// </summary>
	public ServerPackets FriendsList(List<int> friends)
	{
		WritePacketData(ServerPacketId.FriendsList, new PacketData(friends, DataType.IntList));
		return this;
	}
	
	/// <summary>
	/// Packet id 75
	/// </summary>
	public ServerPackets ProtocolVersion(int version)
	{
		WritePacketData(ServerPacketId.ProtocolVersion, new PacketData(version, DataType.Int));
		return this;
	}
	
	/// <summary>
	/// Packet id 76
	/// </summary>
	public ServerPackets MainMenuIcon(string iconUrl, string onclickUrl)
	{
		WritePacketData(ServerPacketId.MainMenuIcon, new PacketData($"{iconUrl}|{onclickUrl}", DataType.String));
		return this;
	}

	/// <summary>
	/// Packet id 80
	/// </summary>
	[Obsolete("No longer used by osu! client.")]
	public ServerPackets Monitor()
	{
		WritePacketData(ServerPacketId.Monitor);
		return this;
	}

	/// <summary>
	/// Packet id 83
	/// </summary>
	public ServerPackets BotPresence(User player)
	{
		WritePacketData(ServerPacketId.UserPresence, new PacketData(player, DataType.BotPresence));
		return this;
	}
	
	/// <summary>
	/// Packet id 83
	/// </summary>
	public ServerPackets UserPresence(User player)
	{
		WritePacketData(ServerPacketId.UserPresence, new PacketData(player, DataType.Presence));
		return this;
	}
	
	/// <summary>
	/// Packet id 86
	/// </summary>
	public ServerPackets RestartServer(int msToReconnect)
	{
		WritePacketData(ServerPacketId.Restart, new PacketData(msToReconnect, DataType.Int));
		return this;
	}
	
	/// <summary>
	/// Packet id 92
	/// </summary>
	public ServerPackets SilenceEnd(int delta)
	{
		WritePacketData(ServerPacketId.SilenceEnd, new PacketData(delta, DataType.Int));
		return this;
	}

	/// <summary>
	/// Packet id 94
	/// </summary>
	public ServerPackets UserSilenced(int playerId)
	{
		WritePacketData(ServerPacketId.UserSilenced, new PacketData(playerId, DataType.Int));
		return this;
	}

	/// <summary>
	/// Packet id 95
	/// </summary>
	[Obsolete("No longer used by osu! client.")]
	public ServerPackets UserPresenceSingle(int playerId)
	{
		WritePacketData(ServerPacketId.UserPresenceSingle, new PacketData(playerId, DataType.Int));
		return this;
	}
	
	/// <summary>
	/// Packet id 96
	/// </summary>
	[Obsolete("No longer used by osu! client.")]
	public ServerPackets UserPresenceBundle(List<int> playerIds)
	{
		WritePacketData(ServerPacketId.UserPresenceBundle, new PacketData(playerIds, DataType.IntList));
		return this;
	}

	/// <summary>
	/// Packet id 100
	/// </summary>
	public ServerPackets UserDmBlocked(string target)
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
	public ServerPackets TargetSilenced(string target)
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
	public ServerPackets VersionUpdateForced()
	{
		WritePacketData(ServerPacketId.VersionUpdateForced);
		return this;
	}

	/// <summary>
	/// Packet id 103
	/// </summary>
	public ServerPackets SwitchServer(int time)
	{
		WritePacketData(ServerPacketId.SwitchServer, new PacketData(time, DataType.String));
		return this;
	}

	/// <summary>
	/// Packet id 107
	/// </summary>
	public ServerPackets SwitchTournamentServer(string ip)
	{
		WritePacketData(ServerPacketId.SwitchTournamentServer, new PacketData(ip, DataType.String));
		return this;
	}
	
	/// <summary>
	/// Packet id 104
	/// </summary>
	public ServerPackets AccountRestricted()
	{
		WritePacketData(ServerPacketId.AccountRestricted);
		return this;
	}

	/// <summary>
	/// Packet id 105
	/// </summary>
	[Obsolete("Shouldn't be sent to osu! client.")]
	public ServerPackets RTX(string message)
	{
		WritePacketData(ServerPacketId.Rtx, new PacketData(message, DataType.String));
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