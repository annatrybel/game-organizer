namespace GameOrganizer.Api.Seeders
{
    public class SeedManager
    {
        private readonly RoleSeeder _roleSeeder;
        private readonly GenreSeeder _genreSeeder;
        private readonly GameSeeder _gameSeeder;
        private readonly PlatformSeeder _platformSeeder;

        public SeedManager(RoleSeeder roleSeeder, GenreSeeder genreSeeder, GameSeeder gameSeeder, PlatformSeeder platformSeeder)
        {
            _roleSeeder = roleSeeder;
            _genreSeeder = genreSeeder;
            _gameSeeder = gameSeeder;
            _platformSeeder = platformSeeder;
        }

        public async Task Seed()
        {
            await _roleSeeder.SeedRolesAsync();
            await _genreSeeder.SeedGenresAsync();
            await _gameSeeder.SeedGamesAsync();
            await _platformSeeder.SeedPlatformsAsync();
        }
    }
}
