using BanchoNET.Core.Models.Stable.Multiplayer;

namespace BanchoNET.Tests;

public class MatchGameTrackingTests
{
    private const string MapA = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string MapB = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    private const int Alice = 1;
    private const int Bob = 2;

    [Test]
    public void ClaimGameFor_ResolvesToTheCurrentGame()
    {
        var match = NewMatch();
        match.TrackGame(10, MapA);

        Assert.Multiple(() =>
        {
            Assert.That(match.CurrentGameId, Is.EqualTo(10));
            Assert.That(match.ClaimGameFor(MapA, Alice), Is.EqualTo(10));
        });
    }

    [Test]
    public void ClaimGameFor_SendsAStragglerToTheGameTheyActuallyPlayed()
    {
        var match = NewMatch();
        match.TrackGame(10, MapA);
        match.TrackGame(11, MapB);

        // Alice submits her MapA score only after the lobby moved on to MapB
        Assert.That(match.ClaimGameFor(MapA, Alice), Is.EqualTo(10));
    }

    [Test]
    public void ClaimGameFor_SeparatesTwoPlaysOfTheSameMap()
    {
        var match = NewMatch();
        match.TrackGame(10, MapA);
        match.TrackGame(11, MapA);

        Assert.Multiple(() =>
        {
            Assert.That(match.ClaimGameFor(MapA, Alice), Is.EqualTo(11));
            Assert.That(match.ClaimGameFor(MapA, Alice), Is.EqualTo(10));

            // Both places taken, so a third score from Alice has nowhere left to go
            Assert.That(match.ClaimGameFor(MapA, Alice), Is.Null);

            // Bob is unaffected by Alice's claims
            Assert.That(match.ClaimGameFor(MapA, Bob), Is.EqualTo(11));
        });
    }

    [Test]
    public void ClaimGameFor_IgnoresAScoreOnAMapTheLobbyNeverPlayed()
    {
        var match = NewMatch();
        match.TrackGame(10, MapA);

        Assert.That(match.ClaimGameFor(MapB, Alice), Is.Null);
    }

    [Test]
    public void ClaimGameFor_ForgetsGamesOlderThanTheTrackedWindow()
    {
        var match = NewMatch();

        match.TrackGame(10, MapA);
        foreach (var id in new long[] { 11, 12, 13 })
            match.TrackGame(id, MapB);

        Assert.Multiple(() =>
        {
            Assert.That(match.ClaimGameFor(MapA, Alice), Is.Null);
            Assert.That(match.CurrentGameId, Is.EqualTo(13));
        });
    }

    private static MultiplayerMatch NewMatch() => new()
    {
        Name = "test",
        Password = "",
        BeatmapName = "",
        BeatmapMD5 = MapA
    };
}