namespace GameOrganizer.Api.Models.Dto
{
    public class MoveGameRequest
    {
        public int GameId { get; set; }
        public int CurrentCollectionId { get; set; }
        public int TargetCollectionId { get; set; }
    }
}
