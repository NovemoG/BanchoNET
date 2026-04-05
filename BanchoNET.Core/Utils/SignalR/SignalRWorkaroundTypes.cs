using BanchoNET.Core.Abstractions.HubClients.Metadata;
using BanchoNET.Core.Abstractions.HubClients.Multiplayer;
using BanchoNET.Core.Abstractions.HubClients.Multiplayer.Match;
using BanchoNET.Core.Abstractions.HubClients.Multiplayer.Matchmaking;

namespace BanchoNET.Core.Utils.SignalR;

internal static class SignalRWorkaroundTypes
    {
        internal static readonly IReadOnlyList<(Type derivedType, Type baseType)> BaseTypeMapping = [
            // multiplayer
            (typeof(MatchUserRequest.ChangeTeamRequest), typeof(MatchUserRequest)),
            (typeof(MatchUserRequest.StartMatchCountdownRequest), typeof(MatchUserRequest)),
            (typeof(MatchUserRequest.StopCountdownRequest), typeof(MatchUserRequest)),
            (typeof(MatchUserRequest.SetLockStateRequest), typeof(MatchUserRequest)),
            (typeof(MatchUserRequest.RollRequest), typeof(MatchUserRequest)),
            (typeof(MatchServerEvent.CountdownStartedEvent), typeof(MatchServerEvent)),
            (typeof(MatchServerEvent.CountdownStoppedEvent), typeof(MatchServerEvent)),
            (typeof(MatchServerEvent.RollEvent), typeof(MatchServerEvent)),
            (typeof(MatchRoomState.TeamVersusRoomState), typeof(MatchRoomState)),
            (typeof(MatchUserState.TeamVersusUserState), typeof(MatchUserState)),
            (typeof(MultiplayerCountdown.MatchStartCountdown), typeof(MultiplayerCountdown)),
            (typeof(MultiplayerCountdown.ForceGameplayStartCountdown), typeof(MultiplayerCountdown)),
            (typeof(MultiplayerCountdown.ServerShuttingDownCountdown), typeof(MultiplayerCountdown)),

            // metadata
            (typeof(UserActivity.ChoosingBeatmap), typeof(UserActivity)),
            (typeof(UserActivity.InSoloGame), typeof(UserActivity)),
            (typeof(UserActivity.WatchingReplay), typeof(UserActivity)),
            (typeof(UserActivity.SpectatingUser), typeof(UserActivity)),
            (typeof(UserActivity.SearchingForLobby), typeof(UserActivity)),
            (typeof(UserActivity.InLobby), typeof(UserActivity)),
            (typeof(UserActivity.InMultiplayerGame), typeof(UserActivity)),
            (typeof(UserActivity.SpectatingMultiplayerGame), typeof(UserActivity)),
            (typeof(UserActivity.InPlaylistGame), typeof(UserActivity)),
            (typeof(UserActivity.EditingBeatmap), typeof(UserActivity)),
            (typeof(UserActivity.ModdingBeatmap), typeof(UserActivity)),
            (typeof(UserActivity.TestingBeatmap), typeof(UserActivity)),
            (typeof(UserActivity.InDailyChallengeLobby), typeof(UserActivity)),
            (typeof(UserActivity.PlayingDailyChallenge), typeof(UserActivity)),

            // matchmaking
            (typeof(MatchmakingQueueStatus.Searching), typeof(MatchmakingQueueStatus)),
            (typeof(MatchmakingQueueStatus.MatchFound), typeof(MatchmakingQueueStatus)),
            (typeof(MatchmakingQueueStatus.JoiningMatch), typeof(MatchmakingQueueStatus)),
            (typeof(MatchRoomState.MatchmakingRoomState), typeof(MatchRoomState)),
            (typeof(MultiplayerCountdown.MatchmakingStageCountdown), typeof(MultiplayerCountdown)),
            (typeof(MatchUserRequest.MatchmakingAvatarActionRequest), typeof(MatchUserRequest)),
            (typeof(MatchServerEvent.MatchmakingAvatarActionEvent), typeof(MatchServerEvent)),

            // ranked play
            (typeof(MatchRoomState.RankedPlayRoomState), typeof(MatchRoomState)),
            (typeof(MultiplayerCountdown.RankedPlayStageCountdown), typeof(MultiplayerCountdown)),
            (typeof(MatchUserRequest.RankedPlayCardHandReplayRequest), typeof(MatchUserRequest)),
            (typeof(MatchServerEvent.RankedPlayCardHandReplayEvent), typeof(MatchServerEvent))
        ];
    }