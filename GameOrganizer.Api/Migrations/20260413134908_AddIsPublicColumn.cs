using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameOrganizer.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPublicColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"CREATE OR REPLACE VIEW ""UserGamesView"" AS
SELECT 
    ug.""Id"" AS ""UserGameId"",
    ug.""UserId"",
    ug.""AddedAt"",
    g.""Id"" AS ""GameId"",
    g.""Title"",
    g.""Description"",
    gen.""Name"" AS ""GenreName"",
    plat.""Name"" AS ""PlatformName"",
    col.""Id"" AS ""CollectionId"",
    col.""Name"" AS ""CollectionName"",
    col.""IsPublic"" AS ""IsPublic""
FROM ""UserGames"" ug
JOIN ""Games"" g ON ug.""GameId"" = g.""Id""
JOIN ""Genres"" gen ON g.""GenreId"" = gen.""Id""
JOIN ""Platforms"" plat ON g.""PlatformId"" = plat.""Id""
JOIN ""Collections"" col ON ug.""CollectionId"" = col.""Id"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"CREATE OR REPLACE VIEW ""UserGamesView"" AS
SELECT 
    ug.""Id"" AS ""UserGameId"",
    ug.""UserId"",
    ug.""AddedAt"",
    g.""Id"" AS ""GameId"",
    g.""Title"",
    g.""Description"",
    gen.""Name"" AS ""GenreName"",
    plat.""Name"" AS ""PlatformName"",
    col.""Id"" AS ""CollectionId"",
    col.""Name"" AS ""CollectionName""
FROM ""UserGames"" ug
JOIN ""Games"" g ON ug.""GameId"" = g.""Id""
JOIN ""Genres"" gen ON g.""GenreId"" = gen.""Id""
JOIN ""Platforms"" plat ON g.""PlatformId"" = plat.""Id""
JOIN ""Collections"" col ON ug.""CollectionId"" = col.""Id"";");
        }
    }
}
