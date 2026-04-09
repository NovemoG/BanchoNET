using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BanchoNET.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddLazerReleases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalMisses",
                table: "Stats");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "StartTime",
                table: "Scores",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "OnlineChecksum",
                table: "Scores",
                type: "CHAR(32)",
                unicode: false,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "CHAR(32)",
                oldUnicode: false);

            migrationBuilder.AddColumn<string>(
                name: "LazerMods",
                table: "Scores",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginTime",
                table: "Players",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<DateTime>(
                name: "OsuVersion",
                table: "PlayerLogins",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "Uninstall",
                table: "ClientHashes",
                type: "CHAR(32)",
                unicode: false,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "CHAR",
                oldUnicode: false,
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "OsuPath",
                table: "ClientHashes",
                type: "CHAR(32)",
                unicode: false,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "CHAR",
                oldUnicode: false,
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "DiskSerial",
                table: "ClientHashes",
                type: "CHAR(32)",
                unicode: false,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "CHAR",
                oldUnicode: false,
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "Adapters",
                table: "ClientHashes",
                type: "CHAR(32)",
                unicode: false,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "CHAR",
                oldUnicode: false,
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<DateTime>(
                name: "SubmitDate",
                table: "Beatmaps",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "RankedDate",
                table: "Beatmaps",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdate",
                table: "Beatmaps",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateTable(
                name: "Releases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Type = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    FileName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SHA1 = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SHA256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Releases", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Releases_PublishedAt",
                table: "Releases",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Releases_Type",
                table: "Releases",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Releases");

            migrationBuilder.DropColumn(
                name: "LazerMods",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "LastLoginTime",
                table: "Players");

            migrationBuilder.AddColumn<long>(
                name: "TotalMisses",
                table: "Stats",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartTime",
                table: "Scores",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OnlineChecksum",
                table: "Scores",
                type: "CHAR(32)",
                unicode: false,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "CHAR(32)",
                oldUnicode: false,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "OsuVersion",
                table: "PlayerLogins",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<string>(
                name: "Uninstall",
                table: "ClientHashes",
                type: "CHAR",
                unicode: false,
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "CHAR(32)",
                oldUnicode: false);

            migrationBuilder.AlterColumn<string>(
                name: "OsuPath",
                table: "ClientHashes",
                type: "CHAR",
                unicode: false,
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "CHAR(32)",
                oldUnicode: false);

            migrationBuilder.AlterColumn<string>(
                name: "DiskSerial",
                table: "ClientHashes",
                type: "CHAR",
                unicode: false,
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "CHAR(32)",
                oldUnicode: false);

            migrationBuilder.AlterColumn<string>(
                name: "Adapters",
                table: "ClientHashes",
                type: "CHAR",
                unicode: false,
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "CHAR(32)",
                oldUnicode: false);

            migrationBuilder.AlterColumn<DateTime>(
                name: "SubmitDate",
                table: "Beatmaps",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "RankedDate",
                table: "Beatmaps",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastUpdate",
                table: "Beatmaps",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");
        }
    }
}
