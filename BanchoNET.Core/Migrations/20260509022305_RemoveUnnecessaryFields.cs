using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanchoNET.Core.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnnecessaryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Count100",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "Count300",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "Count50",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "Gekis",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "IgnoreHit",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "IgnoreMiss",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "Katus",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "Misses",
                table: "Scores");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Players",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.CreateIndex(
                name: "IX_Scores_ModKeys",
                table: "Scores",
                column: "ModKeys");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayWatches_Count",
                table: "ReplayWatches",
                column: "Count");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelPlayers_LastReadMessageId",
                table: "ChannelPlayers",
                column: "LastReadMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_BeatmapsetFavorites_FavoriteAt",
                table: "BeatmapsetFavorites",
                column: "FavoriteAt");

            migrationBuilder.CreateIndex(
                name: "IX_BeatmapPlays_Plays",
                table: "BeatmapPlays",
                column: "Plays");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Scores_ModKeys",
                table: "Scores");

            migrationBuilder.DropIndex(
                name: "IX_ReplayWatches_Count",
                table: "ReplayWatches");

            migrationBuilder.DropIndex(
                name: "IX_ChannelPlayers_LastReadMessageId",
                table: "ChannelPlayers");

            migrationBuilder.DropIndex(
                name: "IX_BeatmapsetFavorites_FavoriteAt",
                table: "BeatmapsetFavorites");

            migrationBuilder.DropIndex(
                name: "IX_BeatmapPlays_Plays",
                table: "BeatmapPlays");

            migrationBuilder.AddColumn<int>(
                name: "Count100",
                table: "Scores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Count300",
                table: "Scores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Count50",
                table: "Scores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Gekis",
                table: "Scores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IgnoreHit",
                table: "Scores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IgnoreMiss",
                table: "Scores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Katus",
                table: "Scores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Misses",
                table: "Scores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Players",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);
        }
    }
}
