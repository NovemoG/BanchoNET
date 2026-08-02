using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BanchoNET.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiplayerHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MultiplayerMatches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    HostId = table.Column<int>(type: "integer", nullable: true),
                    Mode = table.Column<byte>(type: "smallint", nullable: false),
                    StartTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MultiplayerMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MultiplayerMatches_Players_HostId",
                        column: x => x.HostId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MultiplayerGames",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchId = table.Column<long>(type: "bigint", nullable: false),
                    BeatmapId = table.Column<int>(type: "integer", nullable: true),
                    BeatmapMD5 = table.Column<string>(type: "CHAR(32)", unicode: false, nullable: false),
                    BeatmapName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Mode = table.Column<byte>(type: "smallint", nullable: false),
                    WinCondition = table.Column<short>(type: "smallint", nullable: false),
                    LobbyType = table.Column<short>(type: "smallint", nullable: false),
                    Mods = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Aborted = table.Column<bool>(type: "boolean", nullable: false),
                    ForceCompleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MultiplayerGames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MultiplayerGames_MultiplayerMatches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "MultiplayerMatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MultiplayerParticipants",
                columns: table => new
                {
                    MatchId = table.Column<long>(type: "bigint", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MultiplayerParticipants", x => new { x.PlayerId, x.MatchId });
                    table.ForeignKey(
                        name: "FK_MultiplayerParticipants_MultiplayerMatches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "MultiplayerMatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MultiplayerParticipants_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MultiplayerEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<short>(type: "smallint", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    GameId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MultiplayerEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MultiplayerEvents_MultiplayerGames_GameId",
                        column: x => x.GameId,
                        principalTable: "MultiplayerGames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MultiplayerEvents_MultiplayerMatches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "MultiplayerMatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MultiplayerEvents_Players_UserId",
                        column: x => x.UserId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MultiplayerScores",
                columns: table => new
                {
                    GameId = table.Column<long>(type: "bigint", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    ScoreId = table.Column<long>(type: "bigint", nullable: true),
                    Team = table.Column<short>(type: "smallint", nullable: false),
                    TotalScore = table.Column<long>(type: "bigint", nullable: false),
                    MaxCombo = table.Column<int>(type: "integer", nullable: false),
                    Accuracy = table.Column<float>(type: "numeric(6,3)", nullable: false),
                    Grade = table.Column<short>(type: "smallint", nullable: false),
                    Mods = table.Column<int>(type: "integer", nullable: false),
                    Count300 = table.Column<int>(type: "integer", nullable: false),
                    Count100 = table.Column<int>(type: "integer", nullable: false),
                    Count50 = table.Column<int>(type: "integer", nullable: false),
                    Gekis = table.Column<int>(type: "integer", nullable: false),
                    Katus = table.Column<int>(type: "integer", nullable: false),
                    Misses = table.Column<int>(type: "integer", nullable: false),
                    Failed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MultiplayerScores", x => new { x.GameId, x.PlayerId });
                    table.ForeignKey(
                        name: "FK_MultiplayerScores_MultiplayerGames_GameId",
                        column: x => x.GameId,
                        principalTable: "MultiplayerGames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MultiplayerScores_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MultiplayerScores_Scores_ScoreId",
                        column: x => x.ScoreId,
                        principalTable: "Scores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MultiplayerEvents_GameId",
                table: "MultiplayerEvents",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_MultiplayerEvents_MatchId_Id",
                table: "MultiplayerEvents",
                columns: new[] { "MatchId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_MultiplayerEvents_UserId",
                table: "MultiplayerEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MultiplayerGames_MatchId",
                table: "MultiplayerGames",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MultiplayerMatches_HostId",
                table: "MultiplayerMatches",
                column: "HostId");

            migrationBuilder.CreateIndex(
                name: "IX_MultiplayerMatches_StartTime",
                table: "MultiplayerMatches",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_MultiplayerParticipants_MatchId",
                table: "MultiplayerParticipants",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MultiplayerScores_PlayerId",
                table: "MultiplayerScores",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_MultiplayerScores_ScoreId",
                table: "MultiplayerScores",
                column: "ScoreId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MultiplayerEvents");

            migrationBuilder.DropTable(
                name: "MultiplayerParticipants");

            migrationBuilder.DropTable(
                name: "MultiplayerScores");

            migrationBuilder.DropTable(
                name: "MultiplayerGames");

            migrationBuilder.DropTable(
                name: "MultiplayerMatches");
        }
    }
}
