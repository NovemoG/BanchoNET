namespace BanchoNET.Packets;

public enum ClientPacketId : short
{
	ChangeAction = 0,
	SendPublicMessage = 1,
	Logout = 2,
	RequestStatusUpdate = 3,
	Ping = 4,
	StartSpectating = 16, //TODO
	StopSpectating = 17, //TODO
	SpectateFrames = 18, //TODO
	ErrorReport = 20, //TODO
	CantSpectate = 21, //TODO
	SendPrivateMessage = 25,
	PartLobby = 29,
	JoinLobby = 30,
	CreateMatch = 31,
	JoinMatch = 32,
	PartMatch = 33,
	MatchChangeSlot = 38,
	MatchReady = 39,
	MatchLock = 40,
	MatchChangeSettings = 41,
	MatchStart = 44,
	MatchScoreUpdate = 47, //TODO
	MatchComplete = 49,
	MatchChangeMods = 51,
	MatchLoadComplete = 52,
	MatchNoBeatmap = 54,
	MatchNotReady = 55,
	MatchFailed = 56,
	MatchHasBeatmap = 59,
	MatchSkipRequest = 60,
	ChannelJoin = 63,
	BeatmapInfoRequest = 68, //TODO
	MatchTransferHost = 70,
	FriendAdd = 73, //TODO
	FriendRemove = 74, //TODO
	MatchChangeTeam = 77,
	ChannelPart = 78,
	ReceiveUpdates = 79,
	SetAwayMessage = 82,
	IrcOnly = 84, //TODO
	UserStatsRequest = 85,
	MatchInvite = 87,
	MatchChangePassword = 90,
	TournamentMatchInfoRequest = 93, //TODO
	UserPresenceRequest = 97, //TODO
	UserPresenceRequestAll = 98, //TODO
	ToggleBlockNonFriendDms = 99, //TODO
	TournamentJoinMatchChannel = 108, //TODO
	TournamentLeaveMatchChannel = 109, //TODO
}