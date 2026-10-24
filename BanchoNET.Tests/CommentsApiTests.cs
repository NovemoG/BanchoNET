using System.Text.Json;
using BanchoNET.Core.Models.Api.Comments;
using BanchoNET.Core.Models.Db;
using BanchoNET.Core.Utils;
using BanchoNET.Core.Utils.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BanchoNET.Tests;

public class CommentsApiTests
{
    [Test]
    public void CommentableMetaSerializesEverythingTheClientMatches()
    {
        var response = new CommentsResponse
        {
            CommentableMeta =
            [
                new CommentableMeta { Id = 261024001, Type = Commentable.BUILD, Title = "Lazer 2026.1024.1" },
                new CommentMeta { Title = "Deleted Item" }
            ]
        };

        using var json = JsonDocument.Parse(JsonSerializer.Serialize<object>(response, SnakeCaseNamingPolicy.Options));
        var meta = json.RootElement.GetProperty("commentable_meta");

        Assert.That(meta[0].GetProperty("id").GetInt32(), Is.EqualTo(261024001));
        Assert.That(meta[0].GetProperty("type").GetString(), Is.EqualTo("build"));
        // The client disables posting unless this object is present.
        Assert.That(meta[0].GetProperty("current_user_attributes").GetProperty("can_new_comment_reason").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(meta[1].TryGetProperty("id", out _), Is.False);
    }

    [TestCase("beatmapset", "beatmapset")]
    [TestCase("Build", "build")]
    [TestCase("news_post", null)]
    [TestCase(null, null)]
    public void CommentableTypesAreParsed(
        string? type,
        string? expected
    ) => Assert.That(Commentable.Parse(type, 1)?.Type, Is.EqualTo(expected));

    [Test]
    public void BuildIdsFitTheClientsCommentableId()
    {
        Assert.That(LazerChangelog.BuildId("2026.1024.1-lazer"), Is.EqualTo(261024001));
        Assert.That(LazerChangelog.BuildId("2099.1231.999-tachyon"), Is.LessThan(int.MaxValue));
    }

    [Test]
    public void BuildThreadMigrationKeepsExistingThreads()
    {
        using var db = new BanchoDbContext(
            new DbContextOptionsBuilder<BanchoDbContext>()
                .UseNpgsql("Host=localhost;Database=comments_tests;Username=test").Options, null!);

        var sql = db.GetService<IMigrator>().GenerateScript("AddBeatmapsetLastApiCheck", "AddBuildCommentThreads");

        Assert.That(sql, Does.Contain("\"BuildId\""));
        Assert.That(sql, Does.Contain("DROP NOT NULL"));
        Assert.That(sql, Does.Not.Contain("DROP COLUMN"));
        Assert.That(sql, Does.Not.Contain("DROP TABLE"));
    }
}
