using BanchoNET.Objects;
using BanchoNET.Objects.Channels;
using BanchoNET.Objects.Players;
using BanchoNET.Services;
using BanchoNET.Utils;

namespace BanchoNET.Packets;

public partial class ServerPackets : Packet
{
	private readonly BinaryWriter _binaryWriter;
	
	public ServerPackets()
	{
		_binaryWriter = new BinaryWriter(DataBuffer);
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
		WritePacketData(ServerPacketId.UserId, playerId);
	}

	/// <summary>
	/// Packet id 7
	/// </summary>
	public void SendMessage(Message message)
	{
		WritePacketData(ServerPacketId.SendMessage, 
			message.Sender,
			message.Content,
			message.Destination,
			message.SenderId
		);
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
		WritePacketData(ServerPacketId.HandleIrcChangeUsername, $"{oldName}>>>>{newName}");
	}
	
	/// <summary>
	/// Packet id 11
	/// </summary>
	public void UserStats(Player player)
	{
		var modeStats = player.Stats[player.Status.Mode];
		var modeRankedScore = modeStats.RankedScore;
		var modePP = modeStats.PP;

		if (modeStats.PP > short.MaxValue)
		{
			modeRankedScore = modePP;
			modePP = 0;
		}
		
		WritePacketData(ServerPacketId.UserStats, 
			player.Id,
			(byte)player.Status.Activity,
			player.Status.ActivityDescription,
			player.Status.BeatmapMD5,
			player.Status.CurrentMods,
			(byte)player.Status.Mode.AsVanilla(),
			player.Status.BeatmapId,
			modeRankedScore,
			modeStats.Accuracy,
			modeStats.PlayCount,
			modeStats.TotalScore,
			modeStats.Rank,
			modePP
		);
	}
	
	//TODO make it modifiable by user
	private static readonly List<(Activity Activity, string Description)> BotStatuses =
	[
		(Activity.Afk, "looking for source.."),
		(Activity.Editing, "the source code.."),
		(Activity.Editing, "server's website.."),
		(Activity.Modding, "your requests.."),
		(Activity.Watching, "over all of you.."),
		(Activity.Watching, "over the server.."),
		(Activity.Testing, "my will to live.."),
		(Activity.Testing, "your patience.."),
		(Activity.Submitting, "scores to database.."),
		(Activity.Submitting, "a pull request.."),
		(Activity.OsuDirect, "updating maps..")
	];
	
	/// <summary>
	/// Packet id 11
	/// </summary>
	public void BotStats(Player player)
	{
		var status = BotStatuses[Random.Shared.Next(0, BotStatuses.Count)];
		
		WritePacketData(ServerPacketId.UserStats, 
			player.Id,
			(byte)status.Activity,
			status.Description,
			"",
			(int)0,
			(byte)0,
			(int)0,
			(long)0,
			0.0f,
			(int)0,
			(long)0,
			(int)0,
			(short)0
		);
	}
	
	/// <summary>
	/// Packet id 12
	/// </summary>
	public void Logout(int playerId)
	{
		WritePacketData(ServerPacketId.UserLogout, playerId, (byte)0);
	}

	/// <summary>
	/// Packet id 13
	/// </summary>
	public void SpectatorJoined(int playerId)
	{
		WritePacketData(ServerPacketId.SpectatorJoined, playerId);
	}

	/// <summary>
	/// Packet id 42
	/// </summary>
	public void FellowSpectatorJoined(int playerId)
	{
		WritePacketData(ServerPacketId.FellowSpectatorJoined, playerId);
	}

	/// <summary>
	/// Packet id 14
	/// </summary>
	public void SpectatorLeft(int playerId)
	{
		WritePacketData(ServerPacketId.SpectatorLeft, playerId);
	}
	
	/// <summary>
	/// Packet id 43
	/// </summary>
	public void FellowSpectatorLeft(int playerId)
	{
		WritePacketData(ServerPacketId.FellowSpectatorLeft, playerId);
	}

	/// <summary>
	/// Packet id 15
	/// </summary>
	[Obsolete]
	public void SpectateFrames()
	{
		
	}

	/// <summary>
	/// Packet id 22
	/// </summary>
	public void SpectatorCantSpectate(int playerId)
	{
		WritePacketData(ServerPacketId.SpectatorCantSpectate, playerId);
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
		WritePacketData(ServerPacketId.Notification, message);
	}

	/// <summary>
	/// Packet id 26
	/// </summary>
	[Obsolete]
	public void UpdateMatch()
	{
		
	}
	
	/// <summary>
	/// Packet id 27
	/// </summary>
	[Obsolete]
	public void NewMatch()
	{
		
	}

	/// <summary>
	/// Packet id 28
	/// </summary>
	[Obsolete]
	public void DisposeMatch()
	{
		
	}

	/// <summary>
	/// Packet id 36
	/// </summary>
	[Obsolete]
	public void MatchJoinSuccess()
	{
		
	}

	/// <summary>
	/// Packet id 37
	/// </summary>
	[Obsolete]
	public void MatchJoinFail()
	{
		
	}
	
	/// <summary>
	/// Packet id 46
	/// </summary>
	[Obsolete]
	public void MatchStart()
	{
		
	}

	/// <summary>
	/// Packet id 48
	/// </summary>
	[Obsolete]
	public void MatchScoreUpdate()
	{
		
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
		WritePacketData(ServerPacketId.AllPlayersLoaded);
	}
	
	/// <summary>
	/// Packet id 57
	/// </summary>
	public void MatchPlayerFailed(int slotId)
	{
		WritePacketData(ServerPacketId.MatchPlayerFailed, slotId);
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
		WritePacketData(ServerPacketId.MatchPlayerSkipped, playerId);
	}

	/// <summary>
	/// Packet id 88
	/// </summary>
	[Obsolete]
	public void MatchInvite(Player player, string targetName)
	{
		
	}

	/// <summary>
	/// Packet id 91
	/// </summary>
	public void MatchChangePassword(string newPwd)
	{
		WritePacketData(ServerPacketId.MatchChangePassword, newPwd);
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
		WritePacketData(ServerPacketId.ChannelJoinSuccess, name);
	}

	/// <summary>
	/// Packet id 65
	/// </summary>
	public void ChannelInfo(Channel channel)
	{
		WritePacketData(ServerPacketId.ChannelInfo,
			channel.Name,
			channel.Description,
			channel.Players.Count
		);
	}
	
	/// <summary>
	/// Packet id 65
	/// </summary>
	public void ChannelInfo(List<Channel> channels)
	{
		var channelData = new List<object>();
		
		foreach (var channel in channels)
		{
			channelData.Add(channel.Name);
			channelData.Add(channel.Description);
			channelData.Add(channel.Players.Count);
		}
		
		WritePacketData(ServerPacketId.ChannelInfo, channelData.ToArray());
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
		WritePacketData(ServerPacketId.ChannelKick, name);
	}

	/// <summary>
	/// Packet id 67
	/// </summary>
	public void ChannelAutoJoin(Channel channel)
	{
		WritePacketData(ServerPacketId.ChannelAutoJoin,
			channel.Name,
			channel.Description,
			channel.Players.Count
		);
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
		WritePacketData(ServerPacketId.Privileges, privileges);
	}

	/// <summary>
	/// Packet id 72
	/// </summary>
	public void FriendsList(List<int> friends)
	{
		var friendsData = new List<object> { friends.Count };
		
		friendsData.AddRange(friends.Cast<object>());

		WritePacketData(ServerPacketId.FriendsList, friendsData.ToArray());
	}
	
	/// <summary>
	/// Packet id 75
	/// </summary>
	public void ProtocolVersion(int version)
	{
		WritePacketData(ServerPacketId.ProtocolVersion, version);
	}
	
	/// <summary>
	/// Packet id 76
	/// </summary>
	public void MainMenuIcon(string iconUrl, string onclickUrl)
	{
		WritePacketData(ServerPacketId.MainMenuIcon, $"{iconUrl}|{onclickUrl}");
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
		WritePacketData(ServerPacketId.UserPresence, 
			player.Id,
			player.Username,
			(byte)1,
			(byte)245,
			(byte)31,
			69.420f,
			727.27f,
			(int)0
		);
	}
	
	/// <summary>
	/// Packet id 83
	/// </summary>
	public void UserPresence(Player player)
	{
		WritePacketData(ServerPacketId.UserPresence, 
			player.Id,
			player.Username,
			player.TimeZone,
			(byte)player.Geoloc.Country.Numeric,
			(byte)((int)player.ToBanchoPrivileges() | ((int)player.Status.Mode.AsVanilla() << 5)),
			player.Geoloc.Longitude,
			player.Geoloc.Latitude,
			player.Stats[player.Status.Mode].Rank
		);
	}
	
	/// <summary>
	/// Packet id 86
	/// </summary>
	public void RestartServer(int msToReconnect)
	{
		WritePacketData(ServerPacketId.Restart, msToReconnect);
	}
	
	/// <summary>
	/// Packet id 92
	/// </summary>
	public void SilenceEnd(int delta)
	{
		WritePacketData(ServerPacketId.UserSilenced, delta);
	}

	/// <summary>
	/// Packet id 94
	/// </summary>
	public void UserSilenced(int playerId)
	{
		WritePacketData(ServerPacketId.UserSilenced, playerId);
	}

	/// <summary>
	/// Packet id 95
	/// </summary>
	[Obsolete("No longer used by osu! client.")]
	public void UserPresenceSingle(int playerId)
	{
		WritePacketData(ServerPacketId.UserPresenceSingle, playerId);
	}
	
	/// <summary>
	/// Packet id 96
	/// </summary>
	[Obsolete("No longer used by osu! client.")]
	public void UserPresenceBundle(IEnumerable<int> playerIds)
	{
		WritePacketData(ServerPacketId.UserPresenceBundle, playerIds.Cast<object>().ToArray());
	}

	/// <summary>
	/// Packet id 100
	/// </summary>
	public void UserDmBlocked(string target)
	{
		// Basically a message to the user that they can't send a message to the target
		WritePacketData(ServerPacketId.UserDmBlocked,
			"",
			"",
			target,
			0
		);
	}

	/// <summary>
	/// Packet id 101
	/// </summary>
	public void TargetSilenced(string target)
	{
		WritePacketData(ServerPacketId.TargetIsSilenced, 
			"",
			"",
			target,
			0
		);
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
		WritePacketData(ServerPacketId.SwitchServer, time);
	}

	/// <summary>
	/// Packet id 107
	/// </summary>
	public void SwitchTournamentServer(string ip)
	{
		WritePacketData(ServerPacketId.SwitchTournamentServer, ip);
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
		WritePacketData(ServerPacketId.Rtx, message);
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
		
		foreach (var restrictedPlayer in session.Restricted)
			restrictedPlayer.Enqueue(loginData);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
			_binaryWriter.Dispose();
		
		base.Dispose(disposing);
	}
}