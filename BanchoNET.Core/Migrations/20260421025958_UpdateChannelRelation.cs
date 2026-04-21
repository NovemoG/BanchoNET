using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanchoNET.Core.Migrations
{
    /// <inheritdoc />
    public partial class UpdateChannelRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChannelDtoPlayerDto");

            migrationBuilder.CreateTable(
                name: "ChannelPlayer",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    ChannelId = table.Column<long>(type: "bigint", nullable: false),
                    LastReadMessageId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelPlayer", x => new { x.ChannelId, x.PlayerId });
                    table.ForeignKey(
                        name: "FK_ChannelPlayer_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "Channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChannelPlayer_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelPlayer_PlayerId",
                table: "ChannelPlayer",
                column: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChannelPlayer");

            migrationBuilder.CreateTable(
                name: "ChannelDtoPlayerDto",
                columns: table => new
                {
                    PlayersId = table.Column<int>(type: "integer", nullable: false),
                    PmChannelsId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelDtoPlayerDto", x => new { x.PlayersId, x.PmChannelsId });
                    table.ForeignKey(
                        name: "FK_ChannelDtoPlayerDto_Channels_PmChannelsId",
                        column: x => x.PmChannelsId,
                        principalTable: "Channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChannelDtoPlayerDto_Players_PlayersId",
                        column: x => x.PlayersId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDtoPlayerDto_PmChannelsId",
                table: "ChannelDtoPlayerDto",
                column: "PmChannelsId");
        }
    }
}
