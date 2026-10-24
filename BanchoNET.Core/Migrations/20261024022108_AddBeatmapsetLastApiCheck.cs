using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanchoNET.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddBeatmapsetLastApiCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastApiCheck",
                table: "Beatmapsets",
                type: "timestamp with time zone",
                nullable: false,
                // existing sets count as checked now, so the deploy doesn't trigger a recheck of every cached set at once
                defaultValueSql: "now()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastApiCheck",
                table: "Beatmapsets");
        }
    }
}
