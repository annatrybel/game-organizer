using GameOrganizer.Api.Models;
using GameOrganizer.Api.Models.DatabaseModels;
using Microsoft.EntityFrameworkCore;

namespace GameOrganizer.Api.Seeders
{
    public class GameSeeder
    {
        private readonly GameOrganizerDbContext _context;

        public GameSeeder(GameOrganizerDbContext context) => _context = context;

        public async Task SeedGamesAsync()
        {
            if (await _context.Games.AnyAsync()) return;

            var genreMap = await _context.Genres.ToDictionaryAsync(g => g.Name, g => g.Id);
            int GetG(string name) => genreMap.ContainsKey(name) ? genreMap[name] : genreMap.Values.First();

            var platformMap = await _context.Platforms.ToDictionaryAsync(p => p.Name, p => p.Id);
            int GetP(string name) => platformMap.ContainsKey(name) ? platformMap[name] : platformMap.Values.First();

            string Img(int id) => $"https://cdn.akamai.steamstatic.com/steam/apps/{id}/header.jpg";

            var gList = new List<Game>();

            int pc = GetP("PC");
            int ps5 = GetP("PS5");
            int xbox = GetP("Xbox Series X");
            int sw = GetP("Nintendo Switch");

            // --- RPG ---
            gList.Add(new Game { Title = "The Witcher 3: Wild Hunt", GenreId = GetG("RPG"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(292030) });
            gList.Add(new Game { Title = "Cyberpunk 2077", GenreId = GetG("RPG"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(1091500) });
            gList.Add(new Game { Title = "Elden Ring", GenreId = GetG("RPG"), PlatformId = ps5, Status = GameStatus.Accepted, ImageUrl = Img(1245620) });
            gList.Add(new Game { Title = "Baldur's Gate 3", GenreId = GetG("RPG"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(1086940) });
            gList.Add(new Game { Title = "Skyrim", GenreId = GetG("RPG"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(489830) });
            gList.Add(new Game { Title = "Fallout 4", GenreId = GetG("RPG"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(377160) });
            gList.Add(new Game { Title = "Starfield", GenreId = GetG("RPG"), PlatformId = xbox, Status = GameStatus.Accepted, ImageUrl = Img(1716740) });
            gList.Add(new Game { Title = "Diablo IV", GenreId = GetG("RPG"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(2344520) });
            gList.Add(new Game { Title = "Mass Effect Legendary Edition", GenreId = GetG("RPG"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(1328670) });
            gList.Add(new Game { Title = "Hades", GenreId = GetG("RPG"), PlatformId = sw, Status = GameStatus.Accepted, ImageUrl = Img(1145360) });
            gList.Add(new Game { Title = "Dark Souls III", GenreId = GetG("RPG"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(374320) });
            gList.Add(new Game { Title = "Dragon Age: Inquisition", GenreId = GetG("RPG"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(1222690) });
            gList.Add(new Game { Title = "Path of Exile", GenreId = GetG("RPG"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(238960) });
            gList.Add(new Game { Title = "Disco Elysium", GenreId = GetG("RPG"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(632470) });
            gList.Add(new Game { Title = "Divinity: Original Sin 2", GenreId = GetG("RPG"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(435150) });
            gList.Add(new Game { Title = "Final Fantasy VII Remake", GenreId = GetG("RPG"), PlatformId = ps5, Status = GameStatus.Accepted, ImageUrl = Img(1462040) });
            gList.Add(new Game { Title = "Persona 5 Royal", GenreId = GetG("RPG"), PlatformId = ps5, Status = GameStatus.Accepted, ImageUrl = Img(1687950) });
            gList.Add(new Game { Title = "Mount & Blade II: Bannerlord", GenreId = GetG("RPG"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(261550) });
            gList.Add(new Game { Title = "Vampire Survivors", GenreId = GetG("RPG"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(1794680) });
            gList.Add(new Game { Title = "Gothic 1 Remake", GenreId = GetG("RPG"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(1290340) });

            // --- Action / Adventure ---
            gList.Add(new Game { Title = "GTA V", GenreId = GetG("Action"), PlatformId = ps5, Status = GameStatus.Accepted, ImageUrl = Img(271590) });
            gList.Add(new Game { Title = "Red Dead Redemption 2", GenreId = GetG("Action"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(1174180) });
            gList.Add(new Game { Title = "Sekiro: Shadows Die Twice", GenreId = GetG("Action"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(814380) });
            gList.Add(new Game { Title = "Monster Hunter: World", GenreId = GetG("Action"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(582010) });
            gList.Add(new Game { Title = "God of War", GenreId = GetG("Action"), PlatformId = ps5, Status = GameStatus.Accepted, ImageUrl = Img(1593500) });
            gList.Add(new Game { Title = "Spider-Man Remastered", GenreId = GetG("Action"), PlatformId = ps5, Status = GameStatus.Accepted, ImageUrl = Img(1817070) });
            gList.Add(new Game { Title = "Horizon Zero Dawn", GenreId = GetG("Action"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(1151640) });
            gList.Add(new Game { Title = "The Last of Us Part I", GenreId = GetG("Action"), PlatformId = ps5, Status = GameStatus.Accepted, ImageUrl = Img(1888140) });
            gList.Add(new Game { Title = "Ghost of Tsushima", GenreId = GetG("Action"), PlatformId = ps5, Status = GameStatus.Accepted, ImageUrl = Img(2215430) });
            gList.Add(new Game { Title = "Devil May Cry 5", GenreId = GetG("Action"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(601150) });
            gList.Add(new Game { Title = "Terraria", GenreId = GetG("Adventure"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(105600) });
            gList.Add(new Game { Title = "Minecraft", GenreId = GetG("Adventure"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = "https://placehold.co/600x400?text=Minecraft" });
            gList.Add(new Game { Title = "Stardew Valley", GenreId = GetG("RPG"), PlatformId = sw, Status = GameStatus.Accepted, ImageUrl = Img(413150) });
            gList.Add(new Game { Title = "Subnautica", GenreId = GetG("Adventure"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(264710) });
            gList.Add(new Game { Title = "Hollow Knight", GenreId = GetG("Action"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(367520) });
            gList.Add(new Game { Title = "Valheim", GenreId = GetG("Action"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(892970) });
            gList.Add(new Game { Title = "Sea of Thieves", GenreId = GetG("Adventure"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(1172620) });
            gList.Add(new Game { Title = "Palworld", GenreId = GetG("Action"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(1623730) });
            gList.Add(new Game { Title = "Enshrouded", GenreId = GetG("Action"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(1203620) });
            gList.Add(new Game { Title = "Star Wars Jedi: Fallen Order", GenreId = GetG("Action"), PlatformId = xbox, Status = GameStatus.Accepted, ImageUrl = Img(1172380) });

            // --- FPS ---
            gList.Add(new Game { Title = "Counter-Strike 2", GenreId = GetG("FPS"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(730) });
            gList.Add(new Game { Title = "Apex Legends", GenreId = GetG("FPS"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(1172470) });
            gList.Add(new Game { Title = "PUBG", GenreId = GetG("FPS"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(578080) });
            gList.Add(new Game { Title = "Rainbow Six Siege", GenreId = GetG("FPS"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(359550) });
            gList.Add(new Game { Title = "Doom Eternal", GenreId = GetG("FPS"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(782330) });
            gList.Add(new Game { Title = "Destiny 2", GenreId = GetG("FPS"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(1085660) });
            gList.Add(new Game { Title = "Warframe", GenreId = GetG("FPS"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(230410) });
            gList.Add(new Game { Title = "Team Fortress 2", GenreId = GetG("FPS"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(440) });
            gList.Add(new Game { Title = "Left 4 Dead 2", GenreId = GetG("FPS"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(550) });
            gList.Add(new Game { Title = "Battlefield V", GenreId = GetG("FPS"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(1238810) });
            gList.Add(new Game { Title = "Halo Infinite", GenreId = GetG("FPS"), PlatformId = xbox, Status = GameStatus.Accepted, ImageUrl = Img(1240440) });
            gList.Add(new Game { Title = "Rust", GenreId = GetG("FPS"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(252490) });
            gList.Add(new Game { Title = "Hunt: Showdown", GenreId = GetG("FPS"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(594650) });
            gList.Add(new Game { Title = "Overwatch 2", GenreId = GetG("FPS"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(2357570) });
            gList.Add(new Game { Title = "Titanfall 2", GenreId = GetG("FPS"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(1237970) });
            gList.Add(new Game { Title = "Far Cry 6", GenreId = GetG("FPS"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(1209340) });
            gList.Add(new Game { Title = "Borderlands 3", GenreId = GetG("FPS"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(397540) });
            gList.Add(new Game { Title = "Call of Duty", GenreId = GetG("FPS"), PlatformId = xbox, Status = GameStatus.Accepted, ImageUrl = Img(1938090) });
            gList.Add(new Game { Title = "Wolfenstein II", GenreId = GetG("FPS"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(612880) });
            gList.Add(new Game { Title = "Metro Exodus", GenreId = GetG("FPS"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(412020) });

            // --- Strategy ---
            gList.Add(new Game { Title = "Civilization VI", GenreId = GetG("Strategy"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(289070) });
            gList.Add(new Game { Title = "Hearts of Iron IV", GenreId = GetG("Strategy"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(394360) });
            gList.Add(new Game { Title = "Stellaris", GenreId = GetG("Strategy"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(281990) });
            gList.Add(new Game { Title = "Cities: Skylines", GenreId = GetG("Strategy"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(255710) });
            gList.Add(new Game { Title = "Total War: Warhammer III", GenreId = GetG("Strategy"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(1142710) });
            gList.Add(new Game { Title = "Age of Empires IV", GenreId = GetG("Strategy"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(1466860) });
            gList.Add(new Game { Title = "Factorio", GenreId = GetG("Strategy"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(427520) });
            gList.Add(new Game { Title = "Europa Universalis IV", GenreId = GetG("Strategy"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(236850) });
            gList.Add(new Game { Title = "XCOM 2", GenreId = GetG("Strategy"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(268500) });
            gList.Add(new Game { Title = "Anno 1800", GenreId = GetG("Strategy"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(916440) });
            gList.Add(new Game { Title = "Frostpunk", GenreId = GetG("Strategy"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(323190) });
            gList.Add(new Game { Title = "Crusader Kings III", GenreId = GetG("Strategy"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(1158310) });
            gList.Add(new Game { Title = "RimWorld", GenreId = GetG("Strategy"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(294100) });
            gList.Add(new Game { Title = "The Sims 4", GenreId = GetG("Strategy"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(1222670) });
            gList.Add(new Game { Title = "Manor Lords", GenreId = GetG("Strategy"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(1363080) });

            // --- Sportowe / Inne / Horror ---
            gList.Add(new Game { Title = "Euro Truck Simulator 2", GenreId = GetG("Action"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(227300) });
            gList.Add(new Game { Title = "FC 24", GenreId = GetG("Sports"), PlatformId = ps5, Status = GameStatus.Accepted, ImageUrl = Img(2195250) });
            gList.Add(new Game { Title = "Rocket League", GenreId = GetG("Sports"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(252950) });
            gList.Add(new Game { Title = "Forza Horizon 5", GenreId = GetG("Sports"), PlatformId = xbox, Status = GameStatus.Accepted, ImageUrl = Img(1551360) });
            gList.Add(new Game { Title = "NBA 2K24", GenreId = GetG("Sports"), PlatformId = ps5, Status = GameStatus.Accepted, ImageUrl = Img(2338770) });
            gList.Add(new Game { Title = "F1 23", GenreId = GetG("Sports"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(2108330) });
            gList.Add(new Game { Title = "Dead by Daylight", GenreId = GetG("Horror"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(381210) });
            gList.Add(new Game { Title = "Phasmophobia", GenreId = GetG("Horror"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(739630) });
            gList.Add(new Game { Title = "Resident Evil 4 Remake", GenreId = GetG("Horror"), PlatformId = ps5, Status = GameStatus.Accepted, ImageUrl = Img(2050650) });
            gList.Add(new Game { Title = "Lethal Company", GenreId = GetG("Horror"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(1966720) });
            gList.Add(new Game { Title = "Among Us", GenreId = GetG("Adventure"), PlatformId = sw, Status = GameStatus.Accepted, ImageUrl = Img(945360) });
            gList.Add(new Game { Title = "Portal 2", GenreId = GetG("Action"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(620) });
            gList.Add(new Game { Title = "Hades II", GenreId = GetG("RPG"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(1145350) });
            gList.Add(new Game { Title = "Project Zomboid", GenreId = GetG("Horror"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(108600) });
            gList.Add(new Game { Title = "DayZ", GenreId = GetG("Horror"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(221100) });
            gList.Add(new Game { Title = "Ark: Survival Ascended", GenreId = GetG("Action"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(2378510) });
            gList.Add(new Game { Title = "Fall Guys", GenreId = GetG("Sports"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(1097150) });
            gList.Add(new Game { Title = "Deep Rock Galactic", GenreId = GetG("FPS"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(548430) });
            gList.Add(new Game { Title = "No Man's Sky", GenreId = GetG("Adventure"), PlatformId = ps5, Status = GameStatus.Accepted, ImageUrl = Img(275850) });
            gList.Add(new Game { Title = "Street Fighter 6", GenreId = GetG("Action"), PlatformId = ps5, Status = GameStatus.Accepted, ImageUrl = Img(1364780) });
            gList.Add(new Game { Title = "Mortal Kombat 1", GenreId = GetG("Action"), PlatformId = xbox, Status = GameStatus.Accepted, ImageUrl = Img(1971870) });
            gList.Add(new Game { Title = "Tekken 8", GenreId = GetG("Action"), PlatformId = ps5, Status = GameStatus.Accepted, ImageUrl = Img(1778820) });
            gList.Add(new Game { Title = "Dota 2", GenreId = GetG("Strategy"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = Img(570) });
            gList.Add(new Game { Title = "League of Legends", GenreId = GetG("Strategy"), PlatformId = pc, Status = GameStatus.Accepted, ImageUrl = "https://placehold.co/600x400?text=LeagueOfLegends" });
            gList.Add(new Game { Title = "Helldivers 2", GenreId = GetG("Action"), PlatformId = ps5, Status = GameStatus.Accepted, ImageUrl = Img(553850) });

            _context.Games.AddRange(gList);
            await _context.SaveChangesAsync();
        }
    }
}