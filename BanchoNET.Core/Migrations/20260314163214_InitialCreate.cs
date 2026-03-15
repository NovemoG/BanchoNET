using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BanchoNET.Core.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Channels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Description = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AutoJoin = table.Column<bool>(type: "boolean", nullable: false),
                    Hidden = table.Column<bool>(type: "boolean", nullable: false),
                    ReadOnly = table.Column<bool>(type: "boolean", nullable: false),
                    ReadPrivileges = table.Column<int>(type: "integer", nullable: false),
                    WritePrivileges = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Channels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "character varying(16)", unicode: false, maxLength: 16, nullable: false),
                    SafeName = table.Column<string>(type: "character varying(16)", unicode: false, maxLength: 16, nullable: false),
                    LoginName = table.Column<string>(type: "character varying(16)", unicode: false, maxLength: 16, nullable: false),
                    Email = table.Column<string>(type: "character varying(160)", unicode: false, maxLength: 160, nullable: false),
                    PasswordHash = table.Column<string>(type: "CHAR(60)", unicode: false, nullable: false),
                    Country = table.Column<string>(type: "CHAR(2)", unicode: false, nullable: false),
                    Privileges = table.Column<int>(type: "integer", nullable: false),
                    PmFriendsOnly = table.Column<bool>(type: "boolean", nullable: false),
                    HideOnlineActivity = table.Column<bool>(type: "boolean", nullable: false),
                    Inactive = table.Column<bool>(type: "boolean", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    RemainingSilence = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RemainingSupporter = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HasSupported = table.Column<bool>(type: "boolean", nullable: false),
                    SupporterLevel = table.Column<byte>(type: "smallint", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastActivityTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PreferredMode = table.Column<byte>(type: "smallint", nullable: false),
                    PlayStyle = table.Column<byte>(type: "smallint", nullable: false),
                    AwayMessage = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    UserPageContent = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    ApiKey = table.Column<string>(type: "CHAR", unicode: false, maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Revoked = table.Column<bool>(type: "boolean", nullable: false),
                    ReplacedByToken = table.Column<string>(type: "text", nullable: true),
                    Jti = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SessionVerifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CodeHash = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Used = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionVerifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Beatmapsets",
                columns: table => new
                {
                    SetId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beatmapsets", x => x.SetId);
                    table.ForeignKey(
                        name: "FK_Beatmapsets_Players_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClientHashes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OsuPath = table.Column<string>(type: "CHAR", unicode: false, maxLength: 32, nullable: false),
                    Adapters = table.Column<string>(type: "CHAR", unicode: false, maxLength: 32, nullable: false),
                    Uninstall = table.Column<string>(type: "CHAR", unicode: false, maxLength: 32, nullable: false),
                    DiskSerial = table.Column<string>(type: "CHAR", unicode: false, maxLength: 32, nullable: false),
                    LatestTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientHashes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientHashes_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SenderId = table.Column<int>(type: "integer", nullable: false),
                    ReceiverId = table.Column<int>(type: "integer", nullable: false),
                    Read = table.Column<bool>(type: "boolean", nullable: false),
                    Message = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_Players_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Messages_Players_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerLogins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ip = table.Column<string>(type: "character varying(45)", unicode: false, maxLength: 45, nullable: false),
                    ReleaseStream = table.Column<string>(type: "character varying(11)", unicode: false, maxLength: 11, nullable: false),
                    OsuVersion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LoginTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerLogins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerLogins_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Relationships",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    TargetId = table.Column<int>(type: "integer", nullable: false),
                    Relation = table.Column<byte>(type: "smallint", nullable: false),
                    IsMutual = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Relationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Relationships_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Relationships_Players_TargetId",
                        column: x => x.TargetId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Scores",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BeatmapMD5 = table.Column<string>(type: "CHAR(32)", unicode: false, nullable: false),
                    MapId = table.Column<int>(type: "integer", nullable: false),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    Preserve = table.Column<bool>(type: "boolean", nullable: false),
                    Processed = table.Column<bool>(type: "boolean", nullable: false),
                    Ranked = table.Column<bool>(type: "boolean", nullable: false),
                    HasReplay = table.Column<bool>(type: "boolean", nullable: false),
                    PP = table.Column<float>(type: "numeric(7,3)", nullable: false),
                    Acc = table.Column<float>(type: "numeric(6,3)", nullable: false),
                    MaxCombo = table.Column<int>(type: "integer", nullable: false),
                    Mods = table.Column<int>(type: "integer", nullable: false),
                    Count300 = table.Column<int>(type: "integer", nullable: false),
                    Count100 = table.Column<int>(type: "integer", nullable: false),
                    Count50 = table.Column<int>(type: "integer", nullable: false),
                    Misses = table.Column<int>(type: "integer", nullable: false),
                    Gekis = table.Column<int>(type: "integer", nullable: false),
                    Katus = table.Column<int>(type: "integer", nullable: false),
                    IgnoreHit = table.Column<int>(type: "integer", nullable: false),
                    IgnoreMiss = table.Column<int>(type: "integer", nullable: false),
                    TotalScore = table.Column<int>(type: "integer", nullable: false),
                    ClassicScore = table.Column<int>(type: "integer", nullable: false),
                    TotalScoreWithoutMods = table.Column<int>(type: "integer", nullable: false),
                    LegacyTotalScore = table.Column<int>(type: "integer", nullable: false),
                    Grade = table.Column<byte>(type: "smallint", nullable: false),
                    Status = table.Column<byte>(type: "smallint", nullable: false),
                    Mode = table.Column<byte>(type: "smallint", nullable: false),
                    PlayTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeElapsed = table.Column<int>(type: "integer", nullable: false),
                    ClientFlags = table.Column<int>(type: "integer", nullable: false),
                    LegacyPerfect = table.Column<bool>(type: "boolean", nullable: false),
                    IsPerfectCombo = table.Column<bool>(type: "boolean", nullable: false),
                    OnlineChecksum = table.Column<string>(type: "CHAR(32)", unicode: false, nullable: false),
                    IsRestricted = table.Column<bool>(type: "boolean", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Scores_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Stats",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    Mode = table.Column<byte>(type: "smallint", nullable: false),
                    TotalScore = table.Column<long>(type: "bigint", nullable: false),
                    RankedScore = table.Column<long>(type: "bigint", nullable: false),
                    PP = table.Column<int>(type: "integer", nullable: false),
                    IsRanked = table.Column<bool>(type: "boolean", nullable: false),
                    Accuracy = table.Column<float>(type: "numeric(6,3)", nullable: false),
                    PeakRank = table.Column<int>(type: "integer", nullable: false),
                    PlayCount = table.Column<int>(type: "integer", nullable: false),
                    PlayTime = table.Column<int>(type: "integer", nullable: false),
                    MaxCombo = table.Column<int>(type: "integer", nullable: false),
                    TotalGekis = table.Column<long>(type: "bigint", nullable: false),
                    TotalKatus = table.Column<long>(type: "bigint", nullable: false),
                    Total300s = table.Column<long>(type: "bigint", nullable: false),
                    Total100s = table.Column<long>(type: "bigint", nullable: false),
                    Total50s = table.Column<long>(type: "bigint", nullable: false),
                    TotalMisses = table.Column<long>(type: "bigint", nullable: false),
                    ReplayViews = table.Column<int>(type: "integer", nullable: false),
                    XHCount = table.Column<int>(type: "integer", nullable: false),
                    XCount = table.Column<int>(type: "integer", nullable: false),
                    SHCount = table.Column<int>(type: "integer", nullable: false),
                    SCount = table.Column<int>(type: "integer", nullable: false),
                    ACount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stats", x => new { x.PlayerId, x.Mode });
                    table.ForeignKey(
                        name: "FK_Stats_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Beatmaps",
                columns: table => new
                {
                    MapId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SetId = table.Column<int>(type: "integer", nullable: false),
                    Private = table.Column<bool>(type: "boolean", nullable: false),
                    IsRankedOfficially = table.Column<bool>(type: "boolean", nullable: false),
                    Mode = table.Column<byte>(type: "smallint", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    MD5 = table.Column<string>(type: "CHAR(32)", nullable: false),
                    Artist = table.Column<string>(type: "character varying(128)", unicode: false, maxLength: 128, nullable: false),
                    ArtistUnicode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, defaultValue: ""),
                    Title = table.Column<string>(type: "character varying(128)", unicode: false, maxLength: 128, nullable: false),
                    TitleUnicode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, defaultValue: ""),
                    Name = table.Column<string>(type: "character varying(128)", unicode: false, maxLength: 128, nullable: false),
                    CreatorName = table.Column<string>(type: "character varying(16)", unicode: false, maxLength: 16, nullable: false),
                    CreatorId = table.Column<int>(type: "integer", nullable: false),
                    Tags = table.Column<string>(type: "character varying(1024)", unicode: false, maxLength: 1024, nullable: false, defaultValue: ""),
                    SubmitDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RankedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalLength = table.Column<int>(type: "integer", nullable: false),
                    HitLength = table.Column<int>(type: "integer", nullable: false),
                    MaxCombo = table.Column<int>(type: "integer", nullable: false),
                    Frozen = table.Column<bool>(type: "boolean", nullable: false),
                    HasVideo = table.Column<bool>(type: "boolean", nullable: false),
                    HasStoryboard = table.Column<bool>(type: "boolean", nullable: false),
                    Plays = table.Column<long>(type: "bigint", nullable: false),
                    Passes = table.Column<long>(type: "bigint", nullable: false),
                    Bpm = table.Column<float>(type: "numeric(15,3)", nullable: false),
                    Cs = table.Column<float>(type: "numeric(4,2)", nullable: false),
                    Ar = table.Column<float>(type: "numeric(4,2)", nullable: false),
                    Od = table.Column<float>(type: "numeric(4,2)", nullable: false),
                    Hp = table.Column<float>(type: "numeric(4,2)", nullable: false),
                    StarRating = table.Column<float>(type: "numeric(9,3)", nullable: false),
                    CirclesCount = table.Column<int>(type: "integer", nullable: false),
                    SlidersCount = table.Column<int>(type: "integer", nullable: false),
                    SpinnersCount = table.Column<int>(type: "integer", nullable: false),
                    IgnoreHit = table.Column<int>(type: "integer", nullable: false),
                    LargeTickHit = table.Column<int>(type: "integer", nullable: false),
                    CoverId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beatmaps", x => x.MapId);
                    table.ForeignKey(
                        name: "FK_Beatmaps_Beatmapsets_SetId",
                        column: x => x.SetId,
                        principalTable: "Beatmapsets",
                        principalColumn: "SetId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Beatmaps_Players_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Beatmaps_CreatorId",
                table: "Beatmaps",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Beatmaps_MapId",
                table: "Beatmaps",
                column: "MapId");

            migrationBuilder.CreateIndex(
                name: "IX_Beatmaps_MD5",
                table: "Beatmaps",
                column: "MD5",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Beatmaps_SetId",
                table: "Beatmaps",
                column: "SetId");

            migrationBuilder.CreateIndex(
                name: "IX_Beatmapsets_OwnerId",
                table: "Beatmapsets",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientHashes_PlayerId",
                table: "ClientHashes",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_Read",
                table: "Messages",
                column: "Read");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ReceiverId",
                table: "Messages",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderId",
                table: "Messages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerLogins_PlayerId",
                table: "PlayerLogins",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_ApiKey",
                table: "Players",
                column: "ApiKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_Country",
                table: "Players",
                column: "Country");

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
                name: "IX_Players_Privileges",
                table: "Players",
                column: "Privileges");

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

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_PlayerId",
                table: "Relationships",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_Relation",
                table: "Relationships",
                column: "Relation");

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_TargetId",
                table: "Relationships",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_BeatmapMD5",
                table: "Scores",
                column: "BeatmapMD5");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_LegacyTotalScore",
                table: "Scores",
                column: "LegacyTotalScore");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_Mode",
                table: "Scores",
                column: "Mode");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_Mods",
                table: "Scores",
                column: "Mods");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_OnlineChecksum",
                table: "Scores",
                column: "OnlineChecksum");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_PlayerId",
                table: "Scores",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_PlayTime",
                table: "Scores",
                column: "PlayTime");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_PP",
                table: "Scores",
                column: "PP");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_Status",
                table: "Scores",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Beatmaps");

            migrationBuilder.DropTable(
                name: "Channels");

            migrationBuilder.DropTable(
                name: "ClientHashes");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "PlayerLogins");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "Relationships");

            migrationBuilder.DropTable(
                name: "Scores");

            migrationBuilder.DropTable(
                name: "SessionVerifications");

            migrationBuilder.DropTable(
                name: "Stats");

            migrationBuilder.DropTable(
                name: "Beatmapsets");

            migrationBuilder.DropTable(
                name: "Players");
        }
    }
}
