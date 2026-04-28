using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanchoNET.Core.Migrations
{
    /// <inheritdoc />
    public partial class UpdateScoreModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Scores_Players_PlayerId",
                table: "Scores");

            migrationBuilder.AddColumn<int>(
                name: "BeatmapId",
                table: "Scores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Scores_IsRestricted",
                table: "Scores",
                column: "IsRestricted");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_MapId",
                table: "Scores",
                column: "MapId");

            migrationBuilder.AddForeignKey(
                name: "FK_Scores_Beatmaps_MapId",
                table: "Scores",
                column: "MapId",
                principalTable: "Beatmaps",
                principalColumn: "MapId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Scores_Players_PlayerId",
                table: "Scores",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Scores_Beatmaps_MapId",
                table: "Scores");

            migrationBuilder.DropForeignKey(
                name: "FK_Scores_Players_PlayerId",
                table: "Scores");

            migrationBuilder.DropIndex(
                name: "IX_Scores_IsRestricted",
                table: "Scores");

            migrationBuilder.DropIndex(
                name: "IX_Scores_MapId",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "BeatmapId",
                table: "Scores");

            migrationBuilder.AddForeignKey(
                name: "FK_Scores_Players_PlayerId",
                table: "Scores",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
