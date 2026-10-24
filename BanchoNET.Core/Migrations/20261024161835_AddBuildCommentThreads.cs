using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanchoNET.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddBuildCommentThreads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "BeatmapsetId",
                table: "Threads",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "BuildId",
                table: "Threads",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Threads_BuildId",
                table: "Threads",
                column: "BuildId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Threads_BuildId",
                table: "Threads");

            migrationBuilder.DropColumn(
                name: "BuildId",
                table: "Threads");

            migrationBuilder.AlterColumn<int>(
                name: "BeatmapsetId",
                table: "Threads",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
