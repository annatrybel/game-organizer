namespace GameOrganizer.Api.Seeders
{
    public class SeedManager
    {
        private readonly RoleSeeder _roleSeeder;

        public SeedManager(RoleSeeder roleSeeder)
        {
            _roleSeeder = roleSeeder;
        }

        public async Task Seed()
        {
            await _roleSeeder.SeedRolesAsync();
        }
    }
}
