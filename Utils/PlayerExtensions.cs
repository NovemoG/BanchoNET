using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Players;
using BanchoNET.Objects.Privileges;
using BanchoNET.Packets;

namespace BanchoNET.Utils;

public static class PlayerExtensions
{
	public static string MakeSafe(this string name)
	{
		return name.Replace(" ", "_");
	}
	
	public static ClientPrivileges ToBanchoPrivileges(this Player player)
	{
		var priv = player.Privileges;

		var retPriv = 0;
		foreach (var clientPriv in EnumExtensions.GetValues<ClientPrivileges>())
		{
			if ((priv & (int)clientPriv) == (int)clientPriv) 
				retPriv |= (int)clientPriv;
		}

		return (ClientPrivileges)retPriv;
	}
	
	public static void LeaveMatch(this Player player)
	{
		
	}
	
	public static void RemoveSpectator(this Player player)
	{
		
	}
	
	public static void LeaveChannel(this Player player, Channel channel)
	{
		
	}

	public static void Enqueue(this Player player, ClientPacketId packetId)
	{
		player.Queue ??= new ServerPackets();
		
		switch (packetId)
		{
			case ClientPacketId.ChangeAction:
				break;
			case ClientPacketId.SendPublicMessage:
				break;
			case ClientPacketId.Logout:
				player.Queue.Logout(player.Id);
				break;
			case ClientPacketId.RequestStatusUpdate:
				player.Queue.UserStats(player);
				break;
			case ClientPacketId.Ping:
				return;
			case ClientPacketId.StartSpectating:
				break;
			case ClientPacketId.StopSpectating:
				break;
			case ClientPacketId.SpectateFrames:
				break;
			case ClientPacketId.ErrorReport:
				break;
			case ClientPacketId.CantSpectate:
				break;
			case ClientPacketId.SendPrivateMessage:
				break;
			case ClientPacketId.PartLobby:
				break;
			case ClientPacketId.JoinLobby:
				break;
			case ClientPacketId.CreateMatch:
				break;
			case ClientPacketId.JoinMatch:
				break;
			case ClientPacketId.PartMatch:
				break;
			case ClientPacketId.MatchChangeSlot:
				break;
			case ClientPacketId.MatchReady:
				break;
			case ClientPacketId.MatchLock:
				break;
			case ClientPacketId.MatchChangeSettings:
				break;
			case ClientPacketId.MatchStart:
				break;
			case ClientPacketId.MatchScoreUpdate:
				break;
			case ClientPacketId.MatchComplete:
				break;
			case ClientPacketId.MatchChangeMods:
				break;
			case ClientPacketId.MatchLoadComplete:
				break;
			case ClientPacketId.MatchNoBeatmap:
				break;
			case ClientPacketId.MatchNotReady:
				break;
			case ClientPacketId.MatchFailed:
				break;
			case ClientPacketId.MatchHasBeatmap:
				break;
			case ClientPacketId.MatchSkipRequest:
				break;
			case ClientPacketId.ChannelJoin:
				break;
			case ClientPacketId.BeatmapInfoRequest:
				break;
			case ClientPacketId.MatchTransferHost:
				break;
			case ClientPacketId.FriendAdd:
				break;
			case ClientPacketId.FriendRemove:
				break;
			case ClientPacketId.MatchChangeTeam:
				break;
			case ClientPacketId.ChannelPart:
				break;
			case ClientPacketId.ReceiveUpdates:
				break;
			case ClientPacketId.SetAwayMessage:
				break;
			case ClientPacketId.IrcOnly:
				break;
			case ClientPacketId.UserStatsRequest:
				player.Queue.UserStats(player);
				break;
			case ClientPacketId.MatchInvite:
				break;
			case ClientPacketId.MatchChangePassword:
				break;
			case ClientPacketId.TournamentMatchInfoRequest:
				break;
			case ClientPacketId.UserPresenceRequest:
				player.Queue.UserPresence(player);
				break;
			case ClientPacketId.UserPresenceRequestAll:
				break;
			case ClientPacketId.ToggleBlockNonFriendDms:
				break;
			case ClientPacketId.TournamentJoinMatchChannel:
				break;
			case ClientPacketId.TournamentLeaveMatchChannel:
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(packetId), "Unknown packet id was passed and couldn't be processed.");
		}
	}
	
	public static byte[] Dequeue(this Player player)
	{
		if (player.Queue == null) return [];
		
		var returnBytes = player.Queue.GetContent();
		
		player.Queue.Dispose();
		player.Queue = null;

		return returnBytes;
	}
}