using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanchoNET.Core.Migrations
{
    /// <inheritdoc />
    public partial class BeatmapCollaborators : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int[]>(
                name: "OwnerIds",
                table: "BeatmapSearch",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);
            
            migrationBuilder.Sql("""
                UPDATE "BeatmapSearch" s
                SET "OwnerIds" = array_remove(ARRAY[b."OwnerId"], 0)
                FROM "Beatmaps" b
                WHERE b."Id" = s."Id";
                """);

            migrationBuilder.CreateTable(
                name: "BeatmapCollaborators",
                columns: table => new
                {
                    BeatmapId = table.Column<int>(type: "integer", nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    OwnerName = table.Column<string>(type: "character varying(32)", unicode: false, maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeatmapCollaborators", x => new { x.BeatmapId, x.OwnerId });
                    table.ForeignKey(
                        name: "FK_BeatmapCollaborators_Beatmaps_BeatmapId",
                        column: x => x.BeatmapId,
                        principalTable: "Beatmaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BeatmapSearch_OwnerIds",
                table: "BeatmapSearch",
                column: "OwnerIds")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_BeatmapCollaborators_OwnerId",
                table: "BeatmapCollaborators",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BeatmapCollaborators");

            migrationBuilder.DropIndex(
                name: "IX_BeatmapSearch_OwnerIds",
                table: "BeatmapSearch");

            migrationBuilder.DropColumn(
                name: "OwnerIds",
                table: "BeatmapSearch");
        }
    }
}
