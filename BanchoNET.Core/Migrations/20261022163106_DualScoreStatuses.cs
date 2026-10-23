using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanchoNET.Core.Migrations
{
    /// <inheritdoc />
    public partial class DualScoreStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MissCount",
                table: "Scores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte>(
                name: "TotalScoreStatus",
                table: "Scores",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<bool>(
                name: "LeaderboardPerformancePoints",
                table: "Players",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LeaderboardSorting",
                table: "Players",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Scores_MapId_Mode_Status_PP",
                table: "Scores",
                columns: new[] { "MapId", "Mode", "Status", "PP" });

            migrationBuilder.CreateIndex(
                name: "IX_Scores_MapId_Mode_TotalScoreStatus_TotalScore",
                table: "Scores",
                columns: new[] { "MapId", "Mode", "TotalScoreStatus", "TotalScore" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Scores_MapId_Mode_Status_PP",
                table: "Scores");

            migrationBuilder.DropIndex(
                name: "IX_Scores_MapId_Mode_TotalScoreStatus_TotalScore",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "MissCount",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "TotalScoreStatus",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "LeaderboardPerformancePoints",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "LeaderboardSorting",
                table: "Players");
        }
    }
}
