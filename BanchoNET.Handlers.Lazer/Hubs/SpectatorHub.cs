using System.Collections.Concurrent;
using BanchoNET.Core.Abstractions.HubClients;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Models.Api;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Lazer.Spectator;
using BanchoNET.Core.Models.Lazer.Spectator.Frames;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Handlers.Lazer.Hubs;

public class SpectatorHub(
    ILogger logger,
    ILazerPlayerService players,
    IScoreSubmissionQueue scoreQueue
) : BaseHub<ISpectatorClient>(logger)
{
    private static string SpectatorGroup(int userId) => $"spectator:{userId}";
    private static readonly ConcurrentDictionary<int, ClientSpectatorState> ClientStates = new();
    
    public async Task BeginPlaySession(
        long? scoreToken,
        SpectatorState state,
        IBeatmapHandler beatmaps
    ) {
        if (!TryGetUserId(out var userId)) return;

        if (state.RulesetID == null)
            return;

        if (state.BeatmapID == null)
            return;
        
        var player = players.GetPlayer(userId);
        if (player == null) return;
        
        var beatmap = await beatmaps.GetBeatmap(state.BeatmapID.Value);
        if (beatmap == null) return;

        var clientState = new ClientSpectatorState
        {
            State = state,
            ScoreToken = scoreToken,
            Score = new ApiScore
            {
                Mods = state.Mods.ToArray(),
                RulesetId = state.RulesetID.Value,
                BeatmapId = state.BeatmapID.Value,
                MaximumStatistics = state.MaximumStatistics,
                User = new BasicApiPlayer
                {
                    Id = userId,
                    Username = player.Player.Username
                }
            },
            BeatmapStatus = beatmap.Status
        };
        
        ClientStates[userId] = clientState;
        
        await Clients.Group(SpectatorGroup(userId))
            .UserBeganPlaying(userId, state);
    }

    public async Task SendFrameData(
        FrameDataBundle data
    ) {
        if (!TryGetUserId(out var userId)) return;
        if (!ClientStates.TryGetValue(userId, out var state)) return;

        var score = state.Score!;
        
        score.Accuracy = data.Header.Accuracy;
        score.Statistics = data.Header.Statistics;
        score.MaxCombo = data.Header.MaxCombo;
        score.Combo = data.Header.Combo;
        score.TotalScore = (int)data.Header.TotalScore; //TODO
        score.Mods = data.Header.Mods!;
        
        state.Frames.AddRange(data.Frames);
        
        await Clients.Group(SpectatorGroup(userId))
            .UserSentFrames(userId, data);
    }

    public async Task EndPlaySession(
        SpectatorState state
    ) {
        if (!TryGetUserId(out var userId)) return;
        
        if (state.State == SpectatedUserState.Playing)
            state.State = SpectatedUserState.Quit;
        
        await Clients.Group(SpectatorGroup(userId))
            .UserFinishedPlaying(userId, state);
        
        ClientStates.TryRemove(userId, out var clientState);
        
        if (clientState?.State == null || clientState.Score == null || clientState.ScoreToken == null)
            return;
        
        /*if (clientState.BeatmapStatus is < BeatmapStatus.Ranked or > BeatmapStatus.Loved)
            return;*/
        
        var score = clientState.Score!;
        if (!score.Statistics.Any(s => s.Key.IsHit() && s.Value > 0))
            return;

        clientState.SubmitTime = DateTime.UtcNow;

        await scoreQueue.EnqueueSpectatorScore(clientState);
    }

    public async Task StartWatchingUser(
        int userId //target
    ) {
        if (!TryGetUserId(out var spectatorId)) return;
        
        var spectator = players.GetPlayer(spectatorId);
        if (spectator == null) return;

        if (ClientStates.TryGetValue(userId, out var existingState) && existingState.State != null)
            await Clients.Caller.UserBeganPlaying(userId, existingState.State);
        
        var spectators = new[] { new SpectatorPlayer
        {
            OnlineId = spectatorId,
            Username = spectator.Player.Username
        }};
        
        var spectatorGroup = SpectatorGroup(userId);
        
        await Groups.AddToGroupAsync(Context.ConnectionId, spectatorGroup);
        await Clients.User(userId.ToString()).UserStartedWatching(spectators);
    }

    public async Task EndWatchingUser(
        int userId //target
    ) {
        if (!TryGetUserId(out var spectatorId)) return;
        
        var spectatorGroup = SpectatorGroup(userId);
        
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, spectatorGroup);
        await Clients.User(userId.ToString()).UserEndedWatching(spectatorId);
    }
}