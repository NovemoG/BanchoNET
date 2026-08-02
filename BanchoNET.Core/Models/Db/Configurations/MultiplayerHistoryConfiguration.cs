using BanchoNET.Core.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BanchoNET.Core.Models.Db.Configurations;

public class MultiplayerHistoryConfiguration :
    IEntityTypeConfiguration<MultiplayerMatchDto>,
    IEntityTypeConfiguration<MultiplayerGameDto>,
    IEntityTypeConfiguration<MultiplayerScoreDto>,
    IEntityTypeConfiguration<MultiplayerEventDto>,
    IEntityTypeConfiguration<MultiplayerParticipantDto>
{
    public void Configure(
        EntityTypeBuilder<MultiplayerMatchDto> builder
    ) {
        builder.ToTable("MultiplayerMatches");

        builder.HasKey(m => m.Id);

        builder.HasIndex(m => m.StartTime);

        builder.Property(m => m.Name)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasOne<PlayerDto>()
            .WithMany()
            .HasForeignKey(m => m.HostId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(
        EntityTypeBuilder<MultiplayerGameDto> builder
    ) {
        builder.ToTable("MultiplayerGames");

        builder.HasKey(g => g.Id);

        builder.HasIndex(g => g.MatchId);

        builder.Property(g => g.BeatmapMD5)
            .IsRequired()
            .HasColumnType("CHAR(32)")
            .IsUnicode(false);

        builder.Property(g => g.BeatmapName)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(g => g.WinCondition)
            .HasConversion<short>();

        builder.Property(g => g.LobbyType)
            .HasConversion<short>();

        builder.HasOne(g => g.Match)
            .WithMany(m => m.Games)
            .HasForeignKey(g => g.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        // BeatmapId is deliberately not a foreign key: a lobby can pick a map this server has
        // never seen, and the game still has to be recorded. The checksum and name snapshot
        // carry everything the scoreboard needs either way.
    }

    public void Configure(
        EntityTypeBuilder<MultiplayerScoreDto> builder
    ) {
        builder.ToTable("MultiplayerScores");

        builder.HasKey(s => new { s.GameId, s.PlayerId });

        // Drives the retention anti-join, which looks scores up by their own id
        builder.HasIndex(s => s.ScoreId);

        builder.Property(s => s.Team)
            .HasConversion<short>();

        builder.Property(s => s.Grade)
            .HasConversion<short>();

        builder.Property(s => s.Accuracy)
            .HasColumnType("numeric(6,3)");

        builder.HasOne(s => s.Game)
            .WithMany(g => g.Scores)
            .HasForeignKey(s => s.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Player)
            .WithMany()
            .HasForeignKey(s => s.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ScoreDto>()
            .WithMany()
            .HasForeignKey(s => s.ScoreId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(
        EntityTypeBuilder<MultiplayerEventDto> builder
    ) {
        builder.ToTable("MultiplayerEvents");

        builder.HasKey(e => e.Id);

        // Reading a match is a range scan over this
        builder.HasIndex(e => new { e.MatchId, e.Id });

        builder.Property(e => e.Type)
            .HasConversion<short>();

        builder.HasOne(e => e.Match)
            .WithMany(m => m.Events)
            .HasForeignKey(e => e.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Game)
            .WithMany()
            .HasForeignKey(e => e.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<PlayerDto>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(
        EntityTypeBuilder<MultiplayerParticipantDto> builder
    ) {
        builder.ToTable("MultiplayerParticipants");

        builder.HasKey(p => new { p.PlayerId, p.MatchId });

        builder.HasIndex(p => p.MatchId);

        builder.HasOne(p => p.Match)
            .WithMany(m => m.Participants)
            .HasForeignKey(p => p.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Player)
            .WithMany()
            .HasForeignKey(p => p.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}