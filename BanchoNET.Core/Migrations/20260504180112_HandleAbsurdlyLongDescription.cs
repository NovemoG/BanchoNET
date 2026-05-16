using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanchoNET.Core.Migrations
{
    /// <inheritdoc />
    public partial class HandleAbsurdlyLongDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Beatmapsets",
                type: "character varying(131072)",
                maxLength: 131072,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(8192)",
                oldMaxLength: 8192,
                oldDefaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Beatmapsets",
                type: "character varying(8192)",
                maxLength: 8192,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(131072)",
                oldMaxLength: 131072,
                oldDefaultValue: "");
        }
    }
}
