using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BanchoNET.Core.Migrations
{
    /// <inheritdoc />
    public partial class BeatmapsRework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Beatmaps_Players_CreatorId",
                table: "Beatmaps");

            migrationBuilder.DropForeignKey(
                name: "FK_Beatmapsets_Players_OwnerId",
                table: "Beatmapsets");

            migrationBuilder.DropForeignKey(
                name: "FK_Scores_Players_PlayerId",
                table: "Scores");

            migrationBuilder.DropIndex(
                name: "IX_Beatmaps_CreatorId",
                table: "Beatmaps");

            migrationBuilder.DropIndex(
                name: "IX_Beatmaps_MapId",
                table: "Beatmaps");

            migrationBuilder.DropColumn(
                name: "BeatmapId",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "Artist",
                table: "Beatmaps");

            migrationBuilder.DropColumn(
                name: "ArtistUnicode",
                table: "Beatmaps");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "Beatmaps");

            migrationBuilder.DropColumn(
                name: "CreatorName",
                table: "Beatmaps");

            migrationBuilder.DropColumn(
                name: "Frozen",
                table: "Beatmaps");

            migrationBuilder.DropColumn(
                name: "HasStoryboard",
                table: "Beatmaps");

            migrationBuilder.DropColumn(
                name: "HasVideo",
                table: "Beatmaps");

            migrationBuilder.DropColumn(
                name: "IgnoreHit",
                table: "Beatmaps");

            migrationBuilder.DropColumn(
                name: "IsRankedOfficially",
                table: "Beatmaps");

            migrationBuilder.DropColumn(
                name: "LargeTickHit",
                table: "Beatmaps");

            migrationBuilder.DropColumn(
                name: "LastUpdate",
                table: "Beatmaps");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Beatmaps");

            migrationBuilder.DropColumn(
                name: "RankedDate",
                table: "Beatmaps");

            migrationBuilder.DropColumn(
                name: "SubmitDate",
                table: "Beatmaps");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Beatmaps");

            migrationBuilder.DropColumn(
                name: "TitleUnicode",
                table: "Beatmaps");

            migrationBuilder.RenameColumn(
                name: "OwnerId",
                table: "Beatmapsets",
                newName: "PlayCount");

            migrationBuilder.RenameColumn(
                name: "SetId",
                table: "Beatmapsets",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_Beatmapsets_OwnerId",
                table: "Beatmapsets",
                newName: "IX_Beatmapsets_PlayCount");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Beatmaps",
                newName: "Version");

            migrationBuilder.RenameColumn(
                name: "Private",
                table: "Beatmaps",
                newName: "IsScoreable");

            migrationBuilder.RenameColumn(
                name: "CoverId",
                table: "Beatmaps",
                newName: "SkillsId");

            migrationBuilder.RenameColumn(
                name: "MapId",
                table: "Beatmaps",
                newName: "Id");

            migrationBuilder.AlterColumn<string>(
                name: "LazerMods",
                table: "Scores",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModKeys",
                table: "Scores",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Statistics",
                table: "Scores",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "SkillsId",
                table: "Players",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Players",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Artist",
                table: "Beatmapsets",
                type: "character varying(128)",
                unicode: false,
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ArtistUnicode",
                table: "Beatmapsets",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<float>(
                name: "Bpm",
                table: "Beatmapsets",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<int>(
                name: "CreatorId",
                table: "Beatmapsets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CreatorName",
                table: "Beatmapsets",
                type: "character varying(16)",
                unicode: false,
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Beatmapsets",
                type: "character varying(8192)",
                maxLength: 8192,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "FavoriteCount",
                table: "Beatmapsets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GenreId",
                table: "Beatmapsets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivateUpload",
                table: "Beatmapsets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRankedOfficially",
                table: "Beatmapsets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsScoreable",
                table: "Beatmapsets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LanguageId",
                table: "Beatmapsets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastUpdated",
                table: "Beatmapsets",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RankedDate",
                table: "Beatmapsets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int[]>(
                name: "Ratings",
                table: "Beatmapsets",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Beatmapsets",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<short>(
                name: "Status",
                table: "Beatmapsets",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<bool>(
                name: "Storyboard",
                table: "Beatmapsets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SubmittedDate",
                table: "Beatmapsets",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Beatmapsets",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Beatmapsets",
                type: "character varying(128)",
                unicode: false,
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TitleUnicode",
                table: "Beatmapsets",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Video",
                table: "Beatmapsets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int[]>(
                name: "Exits",
                table: "Beatmaps",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);

            migrationBuilder.AddColumn<int[]>(
                name: "Fails",
                table: "Beatmaps",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastUpdated",
                table: "Beatmaps",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "MaximumStatistics",
                table: "Beatmaps",
                type: "text",
                nullable: false,
                defaultValue: "");

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

            migrationBuilder.CreateTable(
                name: "BeatmapPlays",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    BeatmapId = table.Column<int>(type: "integer", nullable: false),
                    Plays = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeatmapPlays", x => new { x.PlayerId, x.BeatmapId });
                    table.ForeignKey(
                        name: "FK_BeatmapPlays_Beatmaps_BeatmapId",
                        column: x => x.BeatmapId,
                        principalTable: "Beatmaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BeatmapPlays_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BeatmapsetFavorites",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    BeatmapsetId = table.Column<int>(type: "integer", nullable: false),
                    FavoriteAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeatmapsetFavorites", x => new { x.PlayerId, x.BeatmapsetId });
                    table.ForeignKey(
                        name: "FK_BeatmapsetFavorites_Beatmapsets_BeatmapsetId",
                        column: x => x.BeatmapsetId,
                        principalTable: "Beatmapsets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BeatmapsetFavorites_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReplayWatches",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    ScoreId = table.Column<long>(type: "bigint", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayWatches", x => new { x.PlayerId, x.ScoreId });
                    table.ForeignKey(
                        name: "FK_ReplayWatches_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReplayWatches_Scores_ScoreId",
                        column: x => x.ScoreId,
                        principalTable: "Scores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Skills",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Skills = table.Column<string>(type: "text", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: true),
                    BeatmapId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skills", x => x.Id);
                    table.CheckConstraint("CK_Skills_ExactlyOneOwner", "(\"PlayerId\" IS NOT NULL AND \"BeatmapId\" IS NULL) OR (\"PlayerId\" IS NULL AND \"BeatmapId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Skills_Beatmaps_BeatmapId",
                        column: x => x.BeatmapId,
                        principalTable: "Beatmaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Skills_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Beatmapsets_CreatorId",
                table: "Beatmapsets",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Beatmapsets_Id",
                table: "Beatmapsets",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Beatmapsets_LastUpdated",
                table: "Beatmapsets",
                column: "LastUpdated");

            migrationBuilder.CreateIndex(
                name: "IX_Beatmapsets_RankedDate",
                table: "Beatmapsets",
                column: "RankedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Beatmapsets_Status",
                table: "Beatmapsets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Beatmapsets_SubmittedDate",
                table: "Beatmapsets",
                column: "SubmittedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Beatmaps_Mode",
                table: "Beatmaps",
                column: "Mode");

            migrationBuilder.CreateIndex(
                name: "IX_Beatmaps_Plays",
                table: "Beatmaps",
                column: "Plays");

            migrationBuilder.CreateIndex(
                name: "IX_Beatmaps_Status",
                table: "Beatmaps",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BeatmapOwners_BeatmapId",
                table: "BeatmapOwners",
                column: "BeatmapId");

            migrationBuilder.CreateIndex(
                name: "IX_BeatmapPlays_BeatmapId",
                table: "BeatmapPlays",
                column: "BeatmapId");

            migrationBuilder.CreateIndex(
                name: "IX_BeatmapsetFavorites_BeatmapsetId",
                table: "BeatmapsetFavorites",
                column: "BeatmapsetId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayWatches_ScoreId",
                table: "ReplayWatches",
                column: "ScoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Skills_BeatmapId",
                table: "Skills",
                column: "BeatmapId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Skills_PlayerId",
                table: "Skills",
                column: "PlayerId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Beatmapsets_Players_CreatorId",
                table: "Beatmapsets",
                column: "CreatorId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Scores_Players_PlayerId",
                table: "Scores",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Beatmapsets_Players_CreatorId",
                table: "Beatmapsets");

            migrationBuilder.DropForeignKey(
                name: "FK_Scores_Players_PlayerId",
                table: "Scores");

            migrationBuilder.DropTable(
                name: "BeatmapOwners");

            migrationBuilder.DropTable(
                name: "BeatmapPlays");

            migrationBuilder.DropTable(
                name: "BeatmapsetFavorites");

            migrationBuilder.DropTable(
                name: "ReplayWatches");

            migrationBuilder.DropTable(
                name: "Skills");

            migrationBuilder.DropIndex(
                name: "IX_Beatmapsets_CreatorId",
                table: "Beatmapsets");

            migrationBuilder.DropIndex(
                name: "IX_Beatmapsets_Id",
                table: "Beatmapsets");

            migrationBuilder.DropIndex(
                name: "IX_Beatmapsets_LastUpdated",
                table: "Beatmapsets");

            migrationBuilder.DropIndex(
                name: "IX_Beatmapsets_RankedDate",
                table: "Beatmapsets");

            migrationBuilder.DropIndex(
                name: "IX_Beatmapsets_Status",
                table: "Beatmapsets");

            migrationBuilder.DropIndex(
                name: "IX_Beatmapsets_SubmittedDate",
                table: "Beatmapsets");

            migrationBuilder.DropIndex(
                name: "IX_Beatmaps_Mode",
                table: "Beatmaps");

            migrationBuilder.DropIndex(
                name: "IX_Beatmaps_Plays",
                table: "Beatmaps");

            migrationBuilder.DropIndex(
                name: "IX_Beatmaps_Status",
                table: "Beatmaps");

            migrationBuilder.DropColumn(
                name: "ModKeys",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "Statistics",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "SkillsId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "Artist",
                table: "Beatmapsets");

            migrationBuilder.DropColumn(
                name: "ArtistUnicode",
                table: "Beatmapsets");

            migrationBuilder.DropColumn(
                name: "Bpm",
                table: "Beatmapsets");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "Beatmapsets");

            migrationBuilder.DropColumn(
                name: "CreatorName",
                table: "Beatmapsets");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Beatmapsets");

            migrationBuilder.DropColumn(
                name: "FavoriteCount",
                table: "Beatmapsets");

            migrationBuilder.DropColumn(
                name: "GenreId",
                table: "Beatmapsets");

            migrationBuilder.DropColumn(
                name: "IsPrivateUpload",
                table: "Beatmapsets");

            migrationBuilder.DropColumn(
                name: "IsRankedOfficially",
                table: "Beatmapsets");

            migrationBuilder.DropColumn(
                name: "IsScoreable",
                table: "Beatmapsets");

            migrationBuilder.DropColumn(
                name: "LanguageId",
                table: "Beatmapsets");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "Beatmapsets");

            migrationBuilder.DropColumn(
                name: "RankedDate",
                table: "Beatmapsets");

            migrationBuilder.DropColumn(
                name: "Ratings",
                table: "Beatmapsets");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Beatmapsets");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Beatmapsets");

            migrationBuilder.DropColumn(
                name: "Storyboard",
                table: "Beatmapsets");

            migrationBuilder.DropColumn(
                name: "SubmittedDate",
                table: "Beatmapsets");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Beatmapsets");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Beatmapsets");

            migrationBuilder.DropColumn(
                name: "TitleUnicode",
                table: "Beatmapsets");

            migrationBuilder.DropColumn(
                name: "Video",
                table: "Beatmapsets");

            migrationBuilder.DropColumn(
                name: "Exits",
                table: "Beatmaps");

            migrationBuilder.DropColumn(
                name: "Fails",
                table: "Beatmaps");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "Beatmaps");

            migrationBuilder.DropColumn(
                name: "MaximumStatistics",
                table: "Beatmaps");

            migrationBuilder.RenameColumn(
                name: "PlayCount",
                table: "Beatmapsets",
                newName: "OwnerId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Beatmapsets",
                newName: "SetId");

            migrationBuilder.RenameIndex(
                name: "IX_Beatmapsets_PlayCount",
                table: "Beatmapsets",
                newName: "IX_Beatmapsets_OwnerId");

            migrationBuilder.RenameColumn(
                name: "Version",
                table: "Beatmaps",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "SkillsId",
                table: "Beatmaps",
                newName: "CoverId");

            migrationBuilder.RenameColumn(
                name: "IsScoreable",
                table: "Beatmaps",
                newName: "Private");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Beatmaps",
                newName: "MapId");

            migrationBuilder.AlterColumn<string>(
                name: "LazerMods",
                table: "Scores",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BeatmapId",
                table: "Scores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Artist",
                table: "Beatmaps",
                type: "character varying(128)",
                unicode: false,
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ArtistUnicode",
                table: "Beatmaps",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CreatorId",
                table: "Beatmaps",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CreatorName",
                table: "Beatmaps",
                type: "character varying(16)",
                unicode: false,
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Frozen",
                table: "Beatmaps",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasStoryboard",
                table: "Beatmaps",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasVideo",
                table: "Beatmaps",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "IgnoreHit",
                table: "Beatmaps",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsRankedOfficially",
                table: "Beatmaps",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LargeTickHit",
                table: "Beatmaps",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdate",
                table: "Beatmaps",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Beatmaps",
                type: "character varying(128)",
                unicode: false,
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "RankedDate",
                table: "Beatmaps",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmitDate",
                table: "Beatmaps",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Beatmaps",
                type: "character varying(1024)",
                unicode: false,
                maxLength: 1024,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TitleUnicode",
                table: "Beatmaps",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Beatmaps_CreatorId",
                table: "Beatmaps",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Beatmaps_MapId",
                table: "Beatmaps",
                column: "MapId");

            migrationBuilder.AddForeignKey(
                name: "FK_Beatmaps_Players_CreatorId",
                table: "Beatmaps",
                column: "CreatorId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Beatmapsets_Players_OwnerId",
                table: "Beatmapsets",
                column: "OwnerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Scores_Players_PlayerId",
                table: "Scores",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id");
        }
    }
}
