using BanchoNET.Core.Abstractions.HubClients;
using BanchoNET.Core.Abstractions.Services;
using BanchoNET.Core.Abstractions.Services.Lazer;
using BanchoNET.Core.Models.Api;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Lazer.Spectator;
using BanchoNET.Core.Models.Lazer.Spectator.Frames;
using BanchoNET.Core.Utils.Extensions;

namespace BanchoNET.Handlers.Lazer.Hubs;

public class SpectatorHub(
    ILogger logger,
    ILazerPlayerService players,
    ISpectatorStateStore states,
    IScoreSubmissionQueue scoreQueue
) : BaseHub<ISpectatorClient>(logger)
{
    private static string SpectatorGroup(int userId) => $"spectator:{userId}";

    #region V1 (Obsolete)

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

        if (scoreToken == null)
            return;

        var player = await players.GetPlayer(userId);
        if (player == null) return;

        // lazer re-sends this with the same score token whenever the connection drops and
        // comes back, and it never replays frames it already delivered.
        var existing = await states.Get(scoreToken.Value);
        if (existing != null)
        {
            await states.SetActiveToken(userId, scoreToken.Value);

            await Clients.Group(SpectatorGroup(userId))
                .UserBeganPlaying(userId, state);

            return;
        }

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

        await states.Store(scoreToken.Value, clientState);
        await states.SetActiveToken(userId, scoreToken.Value);

        await Clients.Group(SpectatorGroup(userId))
            .UserBeganPlaying(userId, state);
    }

    public async Task SendFrameData(
        FrameDataBundle data
    ) {
        if (!TryGetUserId(out var userId)) return;

        var token = await states.GetActiveToken(userId);
        if (token == null) return;

        await SendFrameDataV2(token, data);
    }

    public async Task EndPlaySession(
        SpectatorState state
    ) {
        if (!TryGetUserId(out var userId)) return;

        if (state.State == SpectatedUserState.Playing)
            state.State = SpectatedUserState.Quit;

        var token = await states.GetActiveToken(userId);

        await Clients.Group(SpectatorGroup(userId))
            .UserFinishedPlaying(userId, state);

        if (token == null) return;

        await states.ClearActiveToken(userId);
        await ProcessScore(token.Value);
    }

    #endregion

    #region V2

    public async Task BeginPlaySessionV2(
        long? scoreToken,
        SpectatorState state,
        IBeatmapHandler beatmaps
    ) => await BeginPlaySession(scoreToken, state, beatmaps);

    public async Task SendFrameDataV2(
        long? scoreToken,
        FrameDataBundle data
    ) {
        if (!TryGetUserId(out var userId)) return;
        if (scoreToken == null) return;

        var state = await states.Get(scoreToken.Value);
        if (state == null) return;

        if (state.ScoreToken != scoreToken)
        {
            Logger.LogWarning($"{userId} sent frame data with invalid score token. Expected: {state.ScoreToken}, Received: {scoreToken}");
            return;
        }

        var score = state.Score!;

        score.Accuracy = data.Header.Accuracy;
        score.Statistics = data.Header.Statistics;
        score.MaxCombo = data.Header.MaxCombo;
        score.Combo = data.Header.Combo;
        score.TotalScore = (int)data.Header.TotalScore;
        score.Mods = data.Header.Mods!;
        score.TotalScoreWithoutMods = data.Header.TotalScoreWithoutMods;
        score.Pauses = data.Header.Pauses;

        await states.Store(scoreToken.Value, state);
        await states.AppendFrames(scoreToken.Value, data.Frames);

        await Clients.Group(SpectatorGroup(userId))
            .UserSentFrames(userId, data);
    }

    public async Task EndPlaySessionV2(
        long? scoreToken,
        SpectatedUserState finalState
    ) {
        if (!TryGetUserId(out var userId)) return;

        var token = scoreToken ?? await states.GetActiveToken(userId);
        if (token == null) return;

        var clientState = await states.Get(token.Value);
        if (clientState?.State == null) return;

        if (scoreToken != null && clientState.ScoreToken != scoreToken)
        {
            Logger.LogWarning($"{userId} ended play session with invalid score token. Expected: {clientState.ScoreToken}, Received: {scoreToken}");
            return;
        }

        await states.ClearActiveToken(userId);

        if (scoreToken != null)
            await ProcessScore(token.Value);

        if (finalState == SpectatedUserState.Playing)
            finalState = SpectatedUserState.Quit;

        clientState.State.State = finalState;

        await Clients.Group(SpectatorGroup(userId))
            .UserFinishedPlaying(userId, clientState.State);
    }

    #endregion

    public async Task StartWatchingUser(
        int userId //target
    ) {
        if (!TryGetUserId(out var spectatorId)) return;

        var spectator = await players.GetPlayer(spectatorId);
        if (spectator == null) return;

        var token = await states.GetActiveToken(userId);
        if (token != null)
        {
            var existingState = await states.Get(token.Value);
            if (existingState?.State != null)
                await Clients.Caller.UserBeganPlaying(userId, existingState.State);
        }

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

    private async Task ProcessScore(
        long scoreToken
    ) {
        var state = await states.Get(scoreToken);

        var score = state?.Score;
        if (score == null) return;

        /*if (state.BeatmapStatus is < BeatmapStatus.Ranked or > BeatmapStatus.Loved)
            return;*/

        if (!score.Statistics.Any(s => s.Key.IsHit() && s.Value > 0))
        {
            await states.Remove(scoreToken);
            return;
        }

        state!.SubmitTime = DateTime.UtcNow;
        
        await states.Store(scoreToken, state);
        await states.MarkPending(scoreToken);
        
        await scoreQueue.EnqueueSpectatorScore(scoreToken);
    }
}