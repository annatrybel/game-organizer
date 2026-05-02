using GameOrganizer.Api.Models.Dto.GameOrganizer.Api.Models.Dto;

namespace GameOrganizer.Api.Models.Dto
{
    public class GlobalStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalGamesInLibrary { get; set; } 
        public int TotalUserGames { get; set; }    

        public List<StatItemDto> MostPopularGames { get; set; } = new();
        public List<StatItemDto> PopularPlatforms { get; set; } = new();
        public List<StatItemDto> PopularGenres { get; set; } = new();
        public List<StatItemDto> HighestRatedGames { get; set; } = new();
    }
}
