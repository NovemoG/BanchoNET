using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanchoNET.Core.Migrations
{
    /// <inheritdoc />
    public partial class FlatBeatmapOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OwnerId",
                table: "Beatmaps",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OwnerName",
                table: "Beatmaps",
                type: "character varying(32)",
                unicode: false,
                maxLength: 32,
                nullable: false,
                defaultValue: "");
            
            migrationBuilder.Sql("""
                UPDATE "Beatmaps" b
                SET "OwnerName" = bs."CreatorName"
                FROM "Beatmapsets" bs
                WHERE bs."Id" = b."SetId";
                """);

            migrationBuilder.Sql("""
                UPDATE "BeatmapSearch" s
                SET "OwnerNames" = b."OwnerName"
                FROM "Beatmaps" b
                WHERE b."Id" = s."Id";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Beatmaps_OwnerId",
                table: "Beatmaps",
                column: "OwnerId");

            migrationBuilder.DropTable(
                name: "BeatmapOwners");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Beatmaps_OwnerId",
                table: "Beatmaps");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Beatmaps");

            migrationBuilder.DropColumn(
                name: "OwnerName",
                table: "Beatmaps");

            migrationBuilder.CreateTable(
                name: "BeatmapOwners",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    BeatmapId = table.Column<int>(type: "integer", nullable: false),
                    Username = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeatmapOwners", x => new { x.PlayerId, x.BeatmapId });
                    table.ForeignKey(
                        name: "FK_BeatmapOwners_Beatmaps_BeatmapId",
                        column: x => x.BeatmapId,
                        principalTable: "Beatmaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BeatmapOwners_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BeatmapOwners_BeatmapId",
                table: "BeatmapOwners",
                column: "BeatmapId");
        }
    }
}
