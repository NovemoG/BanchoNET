using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace BanchoNET.Core.Migrations
{
    /// <inheritdoc />
    public partial class SearchRatingAndPlaysFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SetPlays",
                table: "BeatmapSearch",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<float>(
                name: "Rating",
                table: "Beatmapsets",
                type: "real",
                nullable: false,
                computedColumnSql: "COALESCE((\r\n    \"Ratings\"[1] * 1 + \"Ratings\"[2] * 2 + \"Ratings\"[3] * 3 + \"Ratings\"[4] * 4 +\r\n    \"Ratings\"[5] * 5 + \"Ratings\"[6] * 6 + \"Ratings\"[7] * 7 + \"Ratings\"[8] * 8 +\r\n    \"Ratings\"[9] * 9 + \"Ratings\"[10] * 10\r\n)::real / NULLIF(\r\n    \"Ratings\"[1] + \"Ratings\"[2] + \"Ratings\"[3] + \"Ratings\"[4] + \"Ratings\"[5] +\r\n    \"Ratings\"[6] + \"Ratings\"[7] + \"Ratings\"[8] + \"Ratings\"[9] + \"Ratings\"[10]\r\n, 0), 0)",
                stored: true);
            
            migrationBuilder.Sql("""
                DELETE FROM "BeatmapOwners" WHERE "PlayerId" = 1 AND "Username" = 'Bancho Bot';
                """);
            
            migrationBuilder.Sql("""
                UPDATE "BeatmapSearch" s
                SET "SetPlays" = bs."PlayCount",
                    "Rating" = bs."Rating",
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
                computedColumnSql: "setweight(to_tsvector('simple', coalesce(\"Title\", '') || ' ' || coalesce(\"TitleUnicode\", '')), 'A') ||\nsetweight(to_tsvector('simple', coalesce(\"Artist\", '') || ' ' || coalesce(\"ArtistUnicode\", '')), 'B') ||\nsetweight(to_tsvector('simple', coalesce(\"CreatorName\", '')), 'C') ||\nsetweight(to_tsvector('simple', coalesce(\"OwnerNames\", '') || ' ' || coalesce(\"Version\", '') || ' ' || coalesce(\"Source\", '') || ' ' || coalesce(\"Tags\", '')), 'D')",
                stored: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldComputedColumnSql: "setweight(to_tsvector('simple', coalesce(\"Title\", '') || ' ' || coalesce(\"TitleUnicode\", '')), 'A') ||\nsetweight(to_tsvector('simple', coalesce(\"Artist\", '') || ' ' || coalesce(\"ArtistUnicode\", '')), 'B') ||\nsetweight(to_tsvector('simple', coalesce(\"CreatorName\", '') || ' ' || coalesce(\"OwnerNames\", '')), 'C') ||\nsetweight(to_tsvector('simple', coalesce(\"Version\", '') || ' ' || coalesce(\"Source\", '') || ' ' || coalesce(\"Tags\", '')), 'D')",
                oldStored: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Beatmapsets");

            migrationBuilder.DropColumn(
                name: "SetPlays",
                table: "BeatmapSearch");

            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "BeatmapSearch",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "setweight(to_tsvector('simple', coalesce(\"Title\", '') || ' ' || coalesce(\"TitleUnicode\", '')), 'A') ||\nsetweight(to_tsvector('simple', coalesce(\"Artist\", '') || ' ' || coalesce(\"ArtistUnicode\", '')), 'B') ||\nsetweight(to_tsvector('simple', coalesce(\"CreatorName\", '') || ' ' || coalesce(\"OwnerNames\", '')), 'C') ||\nsetweight(to_tsvector('simple', coalesce(\"Version\", '') || ' ' || coalesce(\"Source\", '') || ' ' || coalesce(\"Tags\", '')), 'D')",
                stored: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldComputedColumnSql: "setweight(to_tsvector('simple', coalesce(\"Title\", '') || ' ' || coalesce(\"TitleUnicode\", '')), 'A') ||\nsetweight(to_tsvector('simple', coalesce(\"Artist\", '') || ' ' || coalesce(\"ArtistUnicode\", '')), 'B') ||\nsetweight(to_tsvector('simple', coalesce(\"CreatorName\", '')), 'C') ||\nsetweight(to_tsvector('simple', coalesce(\"OwnerNames\", '') || ' ' || coalesce(\"Version\", '') || ' ' || coalesce(\"Source\", '') || ' ' || coalesce(\"Tags\", '')), 'D')",
                oldStored: true);
        }
    }
}
