using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanchoNET.Core.Migrations
{
    /// <inheritdoc />
    public partial class CleanUpChannels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChannelPlayer_Channels_ChannelId",
                table: "ChannelPlayer");

            migrationBuilder.DropForeignKey(
                name: "FK_ChannelPlayer_Players_PlayerId",
                table: "ChannelPlayer");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Players_ReceiverId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_Read",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ReceiverId",
                table: "Messages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChannelPlayer",
                table: "ChannelPlayer");

            migrationBuilder.DropIndex(
                name: "IX_ChannelPlayer_PlayerId",
                table: "ChannelPlayer");

            migrationBuilder.DropColumn(
                name: "Read",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ReceiverId",
                table: "Messages");

            migrationBuilder.RenameTable(
                name: "ChannelPlayer",
                newName: "ChannelPlayers");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChannelPlayers",
                table: "ChannelPlayers",
                columns: new[] { "PlayerId", "ChannelId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelPlayers_ChannelId",
                table: "ChannelPlayers",
                column: "ChannelId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChannelPlayers_Channels_ChannelId",
                table: "ChannelPlayers",
                column: "ChannelId",
                principalTable: "Channels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChannelPlayers_Players_PlayerId",
                table: "ChannelPlayers",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChannelPlayers_Channels_ChannelId",
                table: "ChannelPlayers");

            migrationBuilder.DropForeignKey(
                name: "FK_ChannelPlayers_Players_PlayerId",
                table: "ChannelPlayers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChannelPlayers",
                table: "ChannelPlayers");

            migrationBuilder.DropIndex(
                name: "IX_ChannelPlayers_ChannelId",
                table: "ChannelPlayers");

            migrationBuilder.RenameTable(
                name: "ChannelPlayers",
                newName: "ChannelPlayer");

            migrationBuilder.AddColumn<bool>(
                name: "Read",
                table: "Messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ReceiverId",
                table: "Messages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChannelPlayer",
                table: "ChannelPlayer",
                columns: new[] { "ChannelId", "PlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_Read",
                table: "Messages",
                column: "Read");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ReceiverId",
                table: "Messages",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelPlayer_PlayerId",
                table: "ChannelPlayer",
                column: "PlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChannelPlayer_Channels_ChannelId",
                table: "ChannelPlayer",
                column: "ChannelId",
                principalTable: "Channels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChannelPlayer_Players_PlayerId",
                table: "ChannelPlayer",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Players_ReceiverId",
                table: "Messages",
                column: "ReceiverId",
                principalTable: "Players",
                principalColumn: "Id");
        }
    }
}
