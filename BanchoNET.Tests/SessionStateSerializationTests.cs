using System.Text.Json;
using BanchoNET.Core.Models.Api;
using BanchoNET.Core.Models.Api.Beatmaps;
using BanchoNET.Core.Models.Api.Player;
using BanchoNET.Core.Models.Api.Scores;
using BanchoNET.Core.Models.Lazer.Spectator;
using BanchoNET.Core.Models.Lazer.Spectator.Frames;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils.SignalR;
using MessagePack;

namespace BanchoNET.Tests;

public class SessionStateSerializationTests
{
    [Test]
    public void PendingScore_CarriesFieldsApiScoreMarksJsonIgnore()
    {
        var pending = new PendingScore
        {
            Response = new ScoreResponseDto
            {
                BeatmapId = 42,
                CreatedAt = DateTimeOffset.UtcNow,
                Id = 1234,
                UserId = 7
            },
            Score = new ApiScore
            {
                Id = 99,
                RulesetId = 0,
                Rank = "S",
                Passed = true,
                ClockRate = 1.5d,
                Pauses = [10, 20, 30]
            }
        };

        pending.CaptureInternalState();

        var restored = JsonSerializer.Deserialize<PendingScore>(JsonSerializer.Serialize(pending));

        Assert.That(restored, Is.Not.Null);
        restored!.RestoreInternalState();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(restored.Response.Id, Is.EqualTo(1234));
            Assert.That(restored.Response.BeatmapId, Is.EqualTo(42));
            Assert.That(restored.Score!.Id, Is.EqualTo(99));
            Assert.That(restored.Score.Passed, Is.True);
            Assert.That(restored.Score.ClockRate, Is.EqualTo(1.5d));
            Assert.That(restored.Score.Pauses, Is.EqualTo([10, 20, 30]));
        }
    }

    [Test]
    public void ClientSpectatorState_RoundTrips()
    {
        var state = new ClientSpectatorState
        {
            ScoreToken = 555,
            SubmitTime = new DateTime(2026, 7, 29, 12, 0, 0, DateTimeKind.Utc),
            State = new SpectatorState
            {
                BeatmapID = 101,
                RulesetID = 2,
                State = SpectatedUserState.Playing,
                MaximumStatistics = new Dictionary<HitResult, int> { [HitResult.Great] = 300 }
            },
            Score = new ApiScore
            {
                Id = 7,
                Rank = "A",
                User = new BasicApiPlayer { Id = 7, Username = "someone" }
            }
        };

        var restored = JsonSerializer.Deserialize<ClientSpectatorState>(JsonSerializer.Serialize(state));

        Assert.That(restored, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(restored!.ScoreToken, Is.EqualTo(555));
            Assert.That(restored.State!.BeatmapID, Is.EqualTo(101));
            Assert.That(restored.State.RulesetID, Is.EqualTo(2));
            Assert.That(restored.State.MaximumStatistics[HitResult.Great], Is.EqualTo(300));
            Assert.That(restored.Score!.User!.Id, Is.EqualTo(7));
            Assert.That(restored.Score.User.Username, Is.EqualTo("someone"));
        }
    }

    [Test]
    public void ReplayFrames_RoundTripThroughMessagePack()
    {
        var frames = new List<LegacyReplayFrame>
        {
            new(0d, 12.5f, 40.25f, ReplayButtonState.Left1),
            new(16.6667d, null, null, ReplayButtonState.None),
            new(9_999.5d, -3f, 512f, ReplayButtonState.Left1 | ReplayButtonState.Right1)
        };

        var restored = frames
            .Select(f => MessagePackSerializer.Serialize(f, SignalRUnionWorkaroundResolver.Options))
            .Select(b => MessagePackSerializer.Deserialize<LegacyReplayFrame>(b, SignalRUnionWorkaroundResolver.Options))
            .ToList();

        Assert.That(restored, Has.Count.EqualTo(frames.Count));
        for (var i = 0; i < frames.Count; i++)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(restored[i].Time, Is.EqualTo(frames[i].Time));
                Assert.That(restored[i].MouseX, Is.EqualTo(frames[i].MouseX));
                Assert.That(restored[i].MouseY, Is.EqualTo(frames[i].MouseY));
                Assert.That(restored[i].ButtonState, Is.EqualTo(frames[i].ButtonState));
            }
        }
    }

    [Test]
    public void LazerPlayer_RoundTrips()
    {
        var player = new LazerPlayer
        {
            Player = new ApiPlayer
            {
                Id = 3,
                Username = "player",
                Playmode = "taiko"
            },
            Friends = [4, 5, 6],
            LastPlayedBeatmapId = 5443307,
            LastPlayedBeatmapExitIndex = 42
        };

        var restored = JsonSerializer.Deserialize<LazerPlayer>(JsonSerializer.Serialize(player));

        Assert.That(restored, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(restored!.Player.Id, Is.EqualTo(3));
            Assert.That(restored.Player.Username, Is.EqualTo("player"));
            Assert.That(restored.Player.Playmode, Is.EqualTo("taiko"));
            Assert.That(restored.Friends, Is.EqualTo(new[] { 4, 5, 6 }));
            Assert.That(restored.LastPlayedBeatmapId, Is.EqualTo(5443307));
            Assert.That(restored.LastPlayedBeatmapExitIndex, Is.EqualTo(42));
        }
    }
}