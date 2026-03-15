using GameOrganizer.Api.Models;
using GameOrganizer.Api.Models.DatabaseModels;

namespace GameOrganizer.Api.Seeders
{
    public class PlatformSeeder
    {
        private readonly GameOrganizerDbContext _context;

        public PlatformSeeder(GameOrganizerDbContext context) => _context = context;

        public async Task SeedPlatformsAsync()
        {
            if (!_context.Platforms.Any())
            {
                _context.Platforms.AddRange(
                    new Platform { Name = "PC" },
                    new Platform { Name = "PS5" },
                    new Platform { Name = "Xbox Series X" },
                    new Platform { Name = "Nintendo Switch" },
                    new Platform { Name = "PS4" }
                );
                await _context.SaveChangesAsync();
            }
        }
    }
}
