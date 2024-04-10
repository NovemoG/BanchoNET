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
	PartLobby = 29, //TODO
	JoinLobby = 30, //TODO
	CreateMatch = 31,
	JoinMatch = 32,
	PartMatch = 33,
	MatchChangeSlot = 38, //TODO
	MatchReady = 39, //TODO
	MatchLock = 40, //TODO
	MatchChangeSettings = 41, //TODO
	MatchStart = 44, //TODO
	MatchScoreUpdate = 47, //TODO
	MatchComplete = 49, //TODO
	MatchChangeMods = 51, //TODO
	MatchLoadComplete = 52, //TODO
	MatchNoBeatmap = 54, //TODO
	MatchNotReady = 55, //TODO
	MatchFailed = 56, //TODO
	MatchHasBeatmap = 59, //TODO
	MatchSkipRequest = 60, //TODO
	ChannelJoin = 63,
	BeatmapInfoRequest = 68, //TODO
	MatchTransferHost = 70, //TODO
	FriendAdd = 73, //TODO
	FriendRemove = 74, //TODO
	MatchChangeTeam = 77, //TODO
	ChannelPart = 78,
	ReceiveUpdates = 79,
	SetAwayMessage = 82,
	IrcOnly = 84, //TODO
	UserStatsRequest = 85,
	MatchInvite = 87, //TODO
	MatchChangePassword = 90, //TODO
	TournamentMatchInfoRequest = 93, //TODO
	UserPresenceRequest = 97, //TODO
	UserPresenceRequestAll = 98, //TODO
	ToggleBlockNonFriendDms = 99, //TODO
	TournamentJoinMatchChannel = 108, //TODO
	TournamentLeaveMatchChannel = 109, //TODO
}