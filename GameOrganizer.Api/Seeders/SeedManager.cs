namespace GameOrganizer.Api.Seeders
{
    public class SeedManager
    {
        private readonly RoleSeeder _roleSeeder;
        private readonly GenreSeeder _genreSeeder;

        public SeedManager(RoleSeeder roleSeeder, GenreSeeder genreSeeder)
        {
            _roleSeeder = roleSeeder;
            _genreSeeder = genreSeeder;
        }

        public async Task Seed()
        {
            await _roleSeeder.SeedRolesAsync();
            await _genreSeeder.SeedGenresAsync();
        }
    }
}
