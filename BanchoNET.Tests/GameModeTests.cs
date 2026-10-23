using BanchoNET.Core.Models;
using BanchoNET.Core.Models.Beatmaps;
using BanchoNET.Core.Models.Dtos;
using BanchoNET.Core.Models.Mods;
using BanchoNET.Core.Models.Players;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Extensions;
using BanchoNET.Core.Utils.Scores;

namespace BanchoNET.Tests;

public class GameModeTests
{
    [Test]
    public void OnlyOfficialRulesetsAreTracked()
    {
        Assert.That(Enum.GetValues<GameMode>().Select(m => (byte)m), Is.EqualTo(new byte[] { 0, 1, 2, 3 }));
        Assert.That(ModeExtensions.TrackedModes, Is.EqualTo(new byte[] { 0, 1, 2, 3 }));
    }

    [TestCase(GameMode.Standard, LegacyMods.Relax)]
    [TestCase(GameMode.Standard, LegacyMods.Autopilot)]
    [TestCase(GameMode.Taiko, LegacyMods.Relax)]
    [TestCase(GameMode.Catch, LegacyMods.Relax)]
    public void StableSubmissionKeepsBaseRulesetAndAssistanceMods(GameMode mode, LegacyMods mods)
    {
        var map = new Beatmap(new BeatmapDto { Id = 1, MD5 = "map", Mode = mode }, null!);
        using var player = new Player(new PlayerDto { Id = 1, Country = "PL", Privileges = 1 });
        string[] data =
        [
            "checksum", "100", "0", "0", "0", "0", "0", "10000", "100",
            "True", "S", ((int)mods).ToString(), "True", ((int)mode).ToString(),
            "261022120000", "0"
        ];
        var score = new Score(data, map, player);
        var submission = ScoreSubmissionMapping.FromLegacy(score, map);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(score.Mode, Is.EqualTo(mode));
            Assert.That(submission.Score.Mode, Is.EqualTo(mode));
            Assert.That(submission.Score.Mods.HasMod(mods), Is.True);
            Assert.That(submission.Score.ModKeys, Does.Contain(mods == LegacyMods.Relax ? "RX;" : "AP;"));
        }
    }
}
