using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace BanchoNET.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Beatmaps",
                columns: table => new
                {
                    MapId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    SetId = table.Column<int>(type: "int", nullable: false),
                    Private = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsRankedOfficially = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Mode = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Status = table.Column<sbyte>(type: "tinyint", nullable: false),
                    MD5 = table.Column<string>(type: "CHAR(32)", unicode: false, nullable: false),
                    Artist = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false),
                    Creator = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    SubmitDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastUpdate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TotalLength = table.Column<int>(type: "int", nullable: false),
                    MaxCombo = table.Column<int>(type: "int", nullable: false),
                    Frozen = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Plays = table.Column<long>(type: "bigint", nullable: false),
                    Passes = table.Column<long>(type: "bigint", nullable: false),
                    Bpm = table.Column<float>(type: "FLOAT(15,3)", nullable: false),
                    Cs = table.Column<float>(type: "FLOAT(4,2)", nullable: false),
                    Ar = table.Column<float>(type: "FLOAT(4,2)", nullable: false),
                    Od = table.Column<float>(type: "FLOAT(4,2)", nullable: false),
                    Hp = table.Column<float>(type: "FLOAT(4,2)", nullable: false),
                    StarRating = table.Column<float>(type: "FLOAT(9,3)", nullable: false),
                    NotesCount = table.Column<int>(type: "int", nullable: false),
                    SlidersCount = table.Column<int>(type: "int", nullable: false),
                    SpinnersCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beatmaps", x => x.MapId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ClientHashes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    OsuPath = table.Column<string>(type: "CHAR(32)", unicode: false, maxLength: 32, nullable: false),
                    Adapters = table.Column<string>(type: "CHAR(32)", unicode: false, maxLength: 32, nullable: false),
                    Uninstall = table.Column<string>(type: "CHAR(32)", unicode: false, maxLength: 32, nullable: false),
                    DiskSerial = table.Column<string>(type: "CHAR(32)", unicode: false, maxLength: 32, nullable: false),
                    LatestTime = table.Column<DateTime>(type: "DATETIME", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientHashes", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlayerLogins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    Ip = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: false),
                    OsuVersion = table.Column<DateTime>(type: "DATETIME", nullable: false),
                    ReleaseStream = table.Column<string>(type: "varchar(11)", unicode: false, maxLength: 11, nullable: false),
                    LoginTime = table.Column<DateTime>(type: "DATETIME", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerLogins", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Username = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    SafeName = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    LoginName = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    Email = table.Column<string>(type: "varchar(160)", unicode: false, maxLength: 160, nullable: false),
                    PasswordHash = table.Column<string>(type: "CHAR(60)", unicode: false, nullable: false),
                    Country = table.Column<string>(type: "CHAR(2)", unicode: false, nullable: false),
                    Privileges = table.Column<int>(type: "int", nullable: false),
                    RemainingSilence = table.Column<int>(type: "int", nullable: false),
                    RemainingSupporter = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "DATETIME", nullable: false),
                    LastActivityTime = table.Column<DateTime>(type: "DATETIME", nullable: false),
                    PreferredMode = table.Column<sbyte>(type: "TINYINT(2)", nullable: false),
                    PlayStyle = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    AwayMessage = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true),
                    UserPageContent = table.Column<string>(type: "varchar(4096)", maxLength: 4096, nullable: true),
                    ApiKey = table.Column<string>(type: "CHAR(36)", unicode: false, maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Relationships",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    TargetId = table.Column<int>(type: "int", nullable: false),
                    Relation = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Relationships", x => x.PlayerId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Scores",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    BeatmapMD5 = table.Column<string>(type: "CHAR(32)", unicode: false, nullable: false),
                    PP = table.Column<float>(type: "FLOAT(7,3)", nullable: false),
                    Acc = table.Column<float>(type: "FLOAT(6,3)", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    MaxCombo = table.Column<int>(type: "int", nullable: false),
                    Mods = table.Column<int>(type: "int", nullable: false),
                    Count300 = table.Column<int>(type: "int", nullable: false),
                    Count100 = table.Column<int>(type: "int", nullable: false),
                    Count50 = table.Column<int>(type: "int", nullable: false),
                    Misses = table.Column<int>(type: "int", nullable: false),
                    Gekis = table.Column<int>(type: "int", nullable: false),
                    Katus = table.Column<int>(type: "int", nullable: false),
                    Grade = table.Column<sbyte>(type: "TINYINT(2)", nullable: false),
                    Status = table.Column<sbyte>(type: "TINYINT(2)", nullable: false),
                    Mode = table.Column<sbyte>(type: "TINYINT(2)", nullable: false),
                    PlayTime = table.Column<DateTime>(type: "DATETIME", nullable: false),
                    TimeElapsed = table.Column<int>(type: "int", nullable: false),
                    ClientFlags = table.Column<int>(type: "int", nullable: false),
                    Perfect = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    OnlineChecksum = table.Column<string>(type: "CHAR(32)", unicode: false, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scores", x => new { x.Id, x.PlayerId });
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Stats",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "int", nullable: false),
                    Mode = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    TotalScore = table.Column<long>(type: "bigint", nullable: false),
                    RankedScore = table.Column<long>(type: "bigint", nullable: false),
                    PP = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Accuracy = table.Column<float>(type: "FLOAT(6,3)", nullable: false),
                    PlayCount = table.Column<int>(type: "int", nullable: false),
                    PlayTime = table.Column<int>(type: "int", nullable: false),
                    MaxCombo = table.Column<int>(type: "int", nullable: false),
                    TotalGekis = table.Column<int>(type: "int", nullable: false),
                    TotalKatus = table.Column<int>(type: "int", nullable: false),
                    Total300s = table.Column<int>(type: "int", nullable: false),
                    Total100s = table.Column<int>(type: "int", nullable: false),
                    Total50s = table.Column<int>(type: "int", nullable: false),
                    ReplaysViews = table.Column<int>(type: "int", nullable: false),
                    XHCount = table.Column<int>(type: "int", nullable: false),
                    XCount = table.Column<int>(type: "int", nullable: false),
                    SHCount = table.Column<int>(type: "int", nullable: false),
                    SCount = table.Column<int>(type: "int", nullable: false),
                    ACount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stats", x => new { x.PlayerId, x.Mode });
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Beatmaps_MD5",
                table: "Beatmaps",
                column: "MD5",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_ApiKey",
                table: "Players",
                column: "ApiKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_Email",
                table: "Players",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_LoginName",
                table: "Players",
                column: "LoginName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_SafeName",
                table: "Players",
                column: "SafeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_Username",
                table: "Players",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Beatmaps");

            migrationBuilder.DropTable(
                name: "ClientHashes");

            migrationBuilder.DropTable(
                name: "PlayerLogins");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Relationships");

            migrationBuilder.DropTable(
                name: "Scores");

            migrationBuilder.DropTable(
                name: "Stats");
        }
    }
}
