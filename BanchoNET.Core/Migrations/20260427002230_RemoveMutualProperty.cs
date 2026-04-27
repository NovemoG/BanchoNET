using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanchoNET.Core.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMutualProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Relationships_PlayerId",
                table: "Relationships");

            migrationBuilder.DropIndex(
                name: "IX_Relationships_Relation",
                table: "Relationships");

            migrationBuilder.DropColumn(
                name: "IsMutual",
                table: "Relationships");

            migrationBuilder.AddColumn<int>(
                name: "TopPlaysCount",
                table: "Players",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_PlayerId_TargetId_Relation",
                table: "Relationships",
                columns: new[] { "PlayerId", "TargetId", "Relation" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Relationships_PlayerId_TargetId_Relation",
                table: "Relationships");

            migrationBuilder.DropColumn(
                name: "TopPlaysCount",
                table: "Players");

            migrationBuilder.AddColumn<bool>(
                name: "IsMutual",
                table: "Relationships",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_PlayerId",
                table: "Relationships",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_Relation",
                table: "Relationships",
                column: "Relation");
        }
    }
}
