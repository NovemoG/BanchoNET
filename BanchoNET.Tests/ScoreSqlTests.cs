using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Models.Scores;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Scores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BanchoNET.Tests;

public class ScoreSqlTests
{
    private static BanchoDbContext Context() => new(
        new DbContextOptionsBuilder<BanchoDbContext>()
            .UseNpgsql("Host=localhost;Database=score_sql_tests;Username=test").Options, null!);

    [Test]
    public void MigrationSnapshotMatchesCurrentModel()
    {
        using var db = Context();
        Assert.That(db.Database.HasPendingModelChanges(), Is.False);
    }

    [Test]
    public void DualStatusMigrationOnlyAddsSchema()
    {
        using var db = Context();
        var sql = db.GetService<IMigrator>().GenerateScript("20261021235051_AddProfileDefaultMods", "DualScoreStatuses");
        Assert.That(sql, Does.Contain("TotalScoreStatus"));
        Assert.That(sql, Does.Contain("LeaderboardPerformancePoints"));
        Assert.That(sql, Does.Not.Contain("DROP COLUMN"));
        Assert.That(sql, Does.Not.Contain("ScoreStorageVersion"));
    }

    [Test]
    public void SharedProcessingMigrationKeepsHistoricalDurationWithoutExtraIdentityStorage()
    {
        using var db = Context();
        var sql = db.GetService<IMigrator>().GenerateScript("DualScoreStatuses", "SharedScoreProcessing");
        Assert.That(sql, Does.Contain("PlayTimeRecorded"));
        Assert.That(sql, Does.Contain("DEFAULT TRUE"));
        Assert.That(sql, Does.Not.Contain("SubmissionKey"));
        Assert.That(sql, Does.Not.Contain("DROP COLUMN"));
        var entity = db.Model.FindEntityType(typeof(Core.Models.Dtos.ScoreDto))!;
        Assert.That(entity.FindProperty("SubmissionKey"), Is.Null);
        var id = entity.FindProperty("Id")!;
        Assert.That(id.GetValueGenerationStrategy(), Is.EqualTo(Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn));
        Assert.That(id.GetBeforeSaveBehavior(), Is.EqualTo(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Save));
    }

    [Test]
    public void ReservedLazerIdIsInsertedAsANewScoreWithoutGeneratingAnotherId()
    {
        using var db = Context();
        var score = new Core.Models.Dtos.ScoreDto { Id = 42, BeatmapMD5 = "", ModKeys = "", LazerMods = "" };
        db.Scores.Add(score);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(db.Entry(score).State, Is.EqualTo(EntityState.Added));
            Assert.That(db.Entry(score).Property(s => s.Id).IsTemporary, Is.False);
            Assert.That(score.Id, Is.EqualTo(42));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SubmissionIdentityLookupTranslatesToPostgres(bool lazer)
    {
        using var db = Context();
        var score = new BanchoNET.Core.Models.Dtos.ScoreDto
        {
            Id = lazer ? 42 : 0, PlayerId = 1, BeatmapMD5 = "", ModKeys = "",
            LazerMods = lazer ? "" : null, OnlineChecksum = lazer ? null : new string('a', 32)
        };
        var sql = db.Scores.Where(ScoreProcessing.MatchSubmission(score)).ToQueryString();
        Assert.That(sql, Does.Contain(lazer ? "\"Id\" =" : "\"OnlineChecksum\" ="));
        Assert.That(sql, Does.Not.Contain("SubmissionKey"));
    }

    [TestCase(LeaderboardSort.Score)]
    [TestCase(LeaderboardSort.PP)]
    [TestCase(LeaderboardSort.Accuracy)]
    [TestCase(LeaderboardSort.MaxCombo)]
    [TestCase(LeaderboardSort.Misses)]
    [TestCase(LeaderboardSort.Date)]
    public void EveryLeaderboardOrderRunsInPostgres(LeaderboardSort sort)
    {
        using var db = Context();
        var sql = LeaderboardOrdering.Order(db.Scores, sort).Take(50).ToQueryString();
        Assert.That(sql, Does.Contain("ORDER BY"));
        Assert.That(sql, Does.Contain("LIMIT"));
        var score = new Core.Models.Dtos.ScoreDto { Id = 1, BeatmapMD5 = "", ModKeys = "" };
        Assert.That(LeaderboardOrdering.Ahead(db.Scores, score, sort).Select(s => s.Id).ToQueryString(), Does.Contain("WHERE"));
    }
}
