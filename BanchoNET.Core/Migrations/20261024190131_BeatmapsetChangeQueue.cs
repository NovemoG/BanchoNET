using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BanchoNET.Core.Migrations
{
    /// <inheritdoc />
    public partial class BeatmapsetChangeQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BeatmapsetChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BeatmapsetId = table.Column<int>(type: "integer", nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeatmapsetChanges", x => x.Id);
                });

            // a set shows its most official beatmap: ranked > approved > qualified > loved > pending > wip > graveyard
            migrationBuilder.Sql("""
                UPDATE "Beatmapsets" AS s
                SET "Status" = d."Status"
                FROM (
                    SELECT DISTINCT ON (b."SetId") b."SetId", b."Status"
                    FROM "Beatmaps" AS b
                    ORDER BY b."SetId",
                             CASE b."Status" WHEN 1 THEN 7 WHEN 2 THEN 6 WHEN 3 THEN 5 WHEN 4 THEN 4 WHEN 0 THEN 3 WHEN -1 THEN 2 ELSE 1 END DESC
                ) AS d
                WHERE d."SetId" = s."Id" AND s."Status" <> d."Status";
                """);

            // sets with a leaderboard keep their ranked date or get their last update, the others have none
            migrationBuilder.Sql("""
                UPDATE "Beatmapsets"
                SET "RankedDate" = "LastUpdated"
                WHERE "RankedDate" IS NULL AND "Status" >= 1;
                """);

            migrationBuilder.Sql("""
                UPDATE "Beatmapsets"
                SET "RankedDate" = NULL
                WHERE "RankedDate" IS NOT NULL AND "Status" < 1;
                """);

            migrationBuilder.Sql("""
                UPDATE "BeatmapSearch" AS r
                SET "RankedDate" = s."RankedDate"
                FROM "Beatmapsets" AS s
                WHERE r."SetId" = s."Id"
                  AND r."RankedDate" IS DISTINCT FROM s."RankedDate";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // the corrected statuses and ranked dates can't be told apart from the old ones, so they're kept
            migrationBuilder.DropTable(
                name: "BeatmapsetChanges");
        }
    }
}
