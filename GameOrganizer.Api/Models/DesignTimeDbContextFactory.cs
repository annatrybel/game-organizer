using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GameOrganizer.Api.Models
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<GameOrganizerDbContext>
    {
        public GameOrganizerDbContext CreateDbContext(string[] args)
        {
            var envPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).FullName, ".env");
            if (File.Exists(envPath))
            {
                DotNetEnv.Env.Load(envPath);
            }

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables() // To wczyta zmienne z pliku .env
                .Build();

            var builder = new DbContextOptionsBuilder<GameOrganizerDbContext>();

            var connectionString = configuration.GetConnectionString("DefaultConnection_LOCAL");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "Nie można odnaleźć connection stringa 'DefaultConnection_LOCAL' dla narzędzi Design-Time. " +
                    "Upewnij się, że jest on zdefiniowany w pliku .env."
                );
            }

            builder.UseNpgsql(connectionString);

            return new GameOrganizerDbContext(builder.Options);
        }
    }
}
