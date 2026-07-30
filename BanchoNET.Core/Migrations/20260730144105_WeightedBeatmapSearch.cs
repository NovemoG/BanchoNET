using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace BanchoNET.Core.Migrations
{
    /// <inheritdoc />
    public partial class WeightedBeatmapSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BeatmapSearch_Title_TitleUnicode_Artist_ArtistUnicode_Versi~",
                table: "BeatmapSearch");

            migrationBuilder.AddColumn<bool>(
                name: "Nsfw",
                table: "Beatmapsets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CreatorId",
                table: "BeatmapSearch",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OwnerNames",
                table: "BeatmapSearch",
                type: "text",
                nullable: false,
                defaultValue: "");
            
            migrationBuilder.Sql("""
                UPDATE "BeatmapSearch" s
                SET "CreatorId" = bs."CreatorId",
                    "Nsfw" = bs."Nsfw",
                    "OwnerNames" = COALESCE((
                        SELECT string_agg(o."Username", ' ')
                        FROM "BeatmapOwners" o
                        WHERE o."BeatmapId" = s."Id"
                    ), '')
                FROM "Beatmapsets" bs
                WHERE bs."Id" = s."SetId";
                """);

            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "BeatmapSearch",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "setweight(to_tsvector('simple', coalesce(\"Title\", '') || ' ' || coalesce(\"TitleUnicode\", '')), 'A') ||\nsetweight(to_tsvector('simple', coalesce(\"Artist\", '') || ' ' || coalesce(\"ArtistUnicode\", '')), 'B') ||\nsetweight(to_tsvector('simple', coalesce(\"CreatorName\", '') || ' ' || coalesce(\"OwnerNames\", '')), 'C') ||\nsetweight(to_tsvector('simple', coalesce(\"Version\", '') || ' ' || coalesce(\"Source\", '') || ' ' || coalesce(\"Tags\", '')), 'D')",
                stored: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector")
                .OldAnnotation("Npgsql:TsVectorConfig", "simple")
                .OldAnnotation("Npgsql:TsVectorProperties", new[] { "Title", "TitleUnicode", "Artist", "ArtistUnicode", "Version", "Source", "Tags", "CreatorName" });

            migrationBuilder.CreateIndex(
                name: "IX_BeatmapSearch_CreatorId",
                table: "BeatmapSearch",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_BeatmapSearch_SetId",
                table: "BeatmapSearch",
                column: "SetId");

            migrationBuilder.CreateIndex(
                name: "IX_BeatmapSearch_Title_TitleUnicode_Artist_ArtistUnicode_Versi~",
                table: "BeatmapSearch",
                columns: new[] { "Title", "TitleUnicode", "Artist", "ArtistUnicode", "Version", "Source", "Tags", "CreatorName", "OwnerNames" })
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops", "gin_trgm_ops", "gin_trgm_ops", "gin_trgm_ops", "gin_trgm_ops", "gin_trgm_ops", "gin_trgm_ops", "gin_trgm_ops", "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BeatmapSearch_CreatorId",
                table: "BeatmapSearch");

            migrationBuilder.DropIndex(
                name: "IX_BeatmapSearch_SetId",
                table: "BeatmapSearch");

            migrationBuilder.DropIndex(
                name: "IX_BeatmapSearch_Title_TitleUnicode_Artist_ArtistUnicode_Versi~",
                table: "BeatmapSearch");

            migrationBuilder.DropColumn(
                name: "Nsfw",
                table: "Beatmapsets");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "BeatmapSearch");

            migrationBuilder.DropColumn(
                name: "OwnerNames",
                table: "BeatmapSearch");

            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "BeatmapSearch",
                type: "tsvector",
                nullable: false,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldComputedColumnSql: "setweight(to_tsvector('simple', coalesce(\"Title\", '') || ' ' || coalesce(\"TitleUnicode\", '')), 'A') ||\nsetweight(to_tsvector('simple', coalesce(\"Artist\", '') || ' ' || coalesce(\"ArtistUnicode\", '')), 'B') ||\nsetweight(to_tsvector('simple', coalesce(\"CreatorName\", '') || ' ' || coalesce(\"OwnerNames\", '')), 'C') ||\nsetweight(to_tsvector('simple', coalesce(\"Version\", '') || ' ' || coalesce(\"Source\", '') || ' ' || coalesce(\"Tags\", '')), 'D')")
                .Annotation("Npgsql:TsVectorConfig", "simple")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Title", "TitleUnicode", "Artist", "ArtistUnicode", "Version", "Source", "Tags", "CreatorName" });

            migrationBuilder.CreateIndex(
                name: "IX_BeatmapSearch_Title_TitleUnicode_Artist_ArtistUnicode_Versi~",
                table: "BeatmapSearch",
                columns: new[] { "Title", "TitleUnicode", "Artist", "ArtistUnicode", "Version", "Source", "Tags", "CreatorName" })
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops", "gin_trgm_ops", "gin_trgm_ops", "gin_trgm_ops", "gin_trgm_ops", "gin_trgm_ops", "gin_trgm_ops", "gin_trgm_ops" });
        }
    }
}
