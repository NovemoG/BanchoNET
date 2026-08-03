using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BanchoNET.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarExtension",
                table: "Players",
                type: "character varying(8)",
                unicode: false,
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AvatarUpdatedAt",
                table: "Players",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverFile",
                table: "Players",
                type: "character varying(64)",
                unicode: false,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CoverPresetId",
                table: "Players",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CoverUpdatedAt",
                table: "Players",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserDiscord",
                table: "Players",
                type: "character varying(37)",
                maxLength: 37,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserFrom",
                table: "Players",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserInterests",
                table: "Players",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UserNotify",
                table: "Players",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "UserOcc",
                table: "Players",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserSig",
                table: "Players",
                type: "character varying(3000)",
                maxLength: 3000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserTwitter",
                table: "Players",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserWebsite",
                table: "Players",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlayerNotificationOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", unicode: false, maxLength: 64, nullable: false),
                    Details = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerNotificationOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerNotificationOptions_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerProfileCustomizations",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    BeatmapsetDownload = table.Column<string>(type: "character varying(8)", unicode: false, maxLength: 8, nullable: false, defaultValue: "All"),
                    BeatmapsetShowNsfw = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    BeatmapsetShowAnimeCover = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    BeatmapsetTitleShowOriginal = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ProfileHue = table.Column<int>(type: "integer", nullable: true),
                    ExtrasOrder = table.Column<string>(type: "jsonb", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerProfileCustomizations", x => x.PlayerId);
                    table.ForeignKey(
                        name: "FK_PlayerProfileCustomizations_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerNotificationOptions_PlayerId_Name",
                table: "PlayerNotificationOptions",
                columns: new[] { "PlayerId", "Name" },
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO "PlayerProfileCustomizations" ("PlayerId", "UpdatedAt")
                SELECT "Id", NOW() AT TIME ZONE 'utc' FROM "Players"
                ON CONFLICT ("PlayerId") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerNotificationOptions");

            migrationBuilder.DropTable(
                name: "PlayerProfileCustomizations");

            migrationBuilder.DropColumn(
                name: "AvatarExtension",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "AvatarUpdatedAt",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "CoverFile",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "CoverPresetId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "CoverUpdatedAt",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "UserDiscord",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "UserFrom",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "UserInterests",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "UserNotify",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "UserOcc",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "UserSig",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "UserTwitter",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "UserWebsite",
                table: "Players");
        }
    }
}
