using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanchoNET.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddPeakRankDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PeakRankDate",
                table: "Stats",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PeakRankDate",
                table: "Stats");
        }
    }
}
