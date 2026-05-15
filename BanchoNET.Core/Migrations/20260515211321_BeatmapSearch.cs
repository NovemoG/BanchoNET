using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;

#nullable disable

namespace BanchoNET.Core.Migrations
{
    /// <inheritdoc />
    public partial class BeatmapSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "BeatmapSearch",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SetId = table.Column<int>(type: "integer", nullable: false),
                    Mode = table.Column<byte>(type: "smallint", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    GenreId = table.Column<int>(type: "integer", nullable: false),
                    LanguageId = table.Column<int>(type: "integer", nullable: false),
                    Nsfw = table.Column<bool>(type: "boolean", nullable: false),
                    StarRating = table.Column<float>(type: "real", nullable: false),
                    Bpm = table.Column<float>(type: "real", nullable: false),
                    Cs = table.Column<float>(type: "real", nullable: false),
                    Ar = table.Column<float>(type: "real", nullable: false),
                    Od = table.Column<float>(type: "real", nullable: false),
                    Hp = table.Column<float>(type: "real", nullable: false),
                    CirclesCount = table.Column<int>(type: "integer", nullable: false),
                    SlidersCount = table.Column<int>(type: "integer", nullable: false),
                    SpinnersCount = table.Column<int>(type: "integer", nullable: false),
                    MaxCombo = table.Column<int>(type: "integer", nullable: false),
                    TotalLength = table.Column<int>(type: "integer", nullable: false),
                    HitLength = table.Column<int>(type: "integer", nullable: false),
                    Plays = table.Column<long>(type: "bigint", nullable: false),
                    Favorites = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<float>(type: "real", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    Artist = table.Column<string>(type: "text", nullable: false),
                    ArtistUnicode = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    TitleUnicode = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CreatorName = table.Column<string>(type: "text", nullable: false),
                    LastUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SubmittedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RankedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SearchVector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: false)
                        .Annotation("Npgsql:TsVectorConfig", "simple")
                        .Annotation("Npgsql:TsVectorProperties", new[] { "Title", "TitleUnicode", "Artist", "ArtistUnicode", "Version", "Source", "Tags", "CreatorName" })
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeatmapSearch", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BeatmapSearch_Bpm_Cs_Ar_Od_Hp_StarRating_Plays_Favorites_Ra~",
                table: "BeatmapSearch",
                columns: new[] { "Bpm", "Cs", "Ar", "Od", "Hp", "StarRating", "Plays", "Favorites", "Rating" });

            migrationBuilder.CreateIndex(
                name: "IX_BeatmapSearch_Mode_Status_GenreId_LanguageId_Nsfw",
                table: "BeatmapSearch",
                columns: new[] { "Mode", "Status", "GenreId", "LanguageId", "Nsfw" });

            migrationBuilder.CreateIndex(
                name: "IX_BeatmapSearch_SearchVector",
                table: "BeatmapSearch",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_BeatmapSearch_Title_TitleUnicode_Artist_ArtistUnicode_Versi~",
                table: "BeatmapSearch",
                columns: new[] { "Title", "TitleUnicode", "Artist", "ArtistUnicode", "Version", "Source", "Tags", "CreatorName" })
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops", "gin_trgm_ops", "gin_trgm_ops", "gin_trgm_ops", "gin_trgm_ops", "gin_trgm_ops", "gin_trgm_ops", "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BeatmapSearch");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");
        }
    }
}
