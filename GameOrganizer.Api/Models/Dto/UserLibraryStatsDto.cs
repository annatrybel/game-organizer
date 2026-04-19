namespace GameOrganizer.Api.Models.Dto
{
    namespace GameOrganizer.Api.Models.Dto
    {
        public class StatItemDto
        {
            public string Label { get; set; } = string.Empty;
            public int Value { get; set; }
        }

        public class UserLibraryStatsDto
        {
            public int TotalGames { get; set; }
            public int AddedRecentlyCount { get; set; }
            public List<StatItemDto> GamesByGenre { get; set; } = new();
            public List<StatItemDto> GamesByPlatform { get; set; } = new();
            public List<StatItemDto> GamesByCollection { get; set; } = new();
        }
    }
}
