namespace GameOrganizer.Api.Seeders
{
    public class SeedManager
    {
        private readonly RoleSeeder _roleSeeder;
        private readonly GenreSeeder _genreSeeder;
        private readonly GameSeeder _gameSeeder;

        public SeedManager(RoleSeeder roleSeeder, GenreSeeder genreSeeder, GameSeeder gameSeeder)
        {
            _roleSeeder = roleSeeder;
            _genreSeeder = genreSeeder;
            _gameSeeder = gameSeeder;
        }

        public async Task Seed()
        {
            await _roleSeeder.SeedRolesAsync();
            await _genreSeeder.SeedGenresAsync();
            await _gameSeeder.SeedGamesAsync();
        }
    }
}
