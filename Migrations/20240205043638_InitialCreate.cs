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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    SetId = table.Column<int>(type: "int", nullable: false),
                    Private = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Mode = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Status = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    MD5 = table.Column<string>(type: "CHAR(32)", unicode: false, nullable: false),
                    Filename = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    Artist = table.Column<string>(type: "longtext", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false),
                    Creator = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    LastUpdate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TotalLength = table.Column<int>(type: "int", nullable: false),
                    MaxCombo = table.Column<int>(type: "int", nullable: false),
                    Frozen = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Plays = table.Column<int>(type: "int", nullable: false),
                    Passes = table.Column<int>(type: "int", nullable: false),
                    Bpm = table.Column<float>(type: "FLOAT(15,3)", nullable: false),
                    Cs = table.Column<float>(type: "FLOAT(4,2)", nullable: false),
                    Ar = table.Column<float>(type: "FLOAT(4,2)", nullable: false),
                    Od = table.Column<float>(type: "FLOAT(4,2)", nullable: false),
                    Hp = table.Column<float>(type: "FLOAT(4,2)", nullable: false),
                    StarRating = table.Column<float>(type: "FLOAT(9,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beatmaps", x => x.Id);
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
                    CreationTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastActivityTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PreferredMode = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    PlayStyle = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    UserPageContent = table.Column<string>(type: "varchar(4096)", maxLength: 4096, nullable: true),
                    ApiKey = table.Column<string>(type: "varchar(255)", unicode: false, nullable: false)
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
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TargetId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Relationships", x => new { x.UserId, x.TargetId });
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
                    PP = table.Column<short>(type: "smallint", nullable: false),
                    Accuracy = table.Column<float>(type: "FLOAT(6,3)", nullable: false),
                    PlayCount = table.Column<int>(type: "int", nullable: false),
                    PlayTime = table.Column<int>(type: "int", nullable: false),
                    MaxCombo = table.Column<int>(type: "int", nullable: false),
                    TotalHits = table.Column<int>(type: "int", nullable: false),
                    Total300s = table.Column<int>(type: "int", nullable: false),
                    Total100s = table.Column<int>(type: "int", nullable: false),
                    Total50s = table.Column<int>(type: "int", nullable: false),
                    ReplayViews = table.Column<int>(type: "int", nullable: false),
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
                name: "Players");

            migrationBuilder.DropTable(
                name: "Relationships");

            migrationBuilder.DropTable(
                name: "Stats");
        }
    }
}
