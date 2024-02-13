using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanchoNET.Migrations
{
    /// <inheritdoc />
    public partial class MinorUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AwayMessage",
                table: "Players",
                type: "varchar(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AwayMessage",
                table: "Players");
        }
    }
}
