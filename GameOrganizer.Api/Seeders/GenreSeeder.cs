using GameOrganizer.Api.Models;
using GameOrganizer.Api.Models.DatabaseModels;

namespace GameOrganizer.Api.Seeders
{
    public class GenreSeeder
    {
        private readonly GameOrganizerDbContext _context;

        public GenreSeeder(GameOrganizerDbContext context)
        {
            _context = context;
        }
        public async Task SeedGenresAsync()
        {
            if (!_context.Genres.Any())
            {
                _context.Genres.AddRange(
                    new Genre { Name = "RPG" },
                    new Genre { Name = "FPS" },
                    new Genre { Name = "Strategy" },
                    new Genre { Name = "Sports" },
                    new Genre { Name = "Action" },
                    new Genre { Name = "Horror" },
                    new Genre { Name = "Adventure" }
                );
                await _context.SaveChangesAsync();
            }
        }
    }
}
