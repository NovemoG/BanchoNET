using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanchoNET.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerHistories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerHistories",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    Mode = table.Column<byte>(type: "smallint", nullable: false),
                    Metric = table.Column<short>(type: "smallint", nullable: false),
                    Granularity = table.Column<short>(type: "smallint", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerHistories", x => new { x.PlayerId, x.Mode, x.Metric, x.Granularity, x.Date });
                    table.ForeignKey(
                        name: "FK_PlayerHistories_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerHistories_Date",
                table: "PlayerHistories",
                column: "Date")
                .Annotation("Npgsql:IndexMethod", "brin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerHistories");
        }
    }
}
