namespace GameOrganizer.Api.Models.DatabaseModels
{
    public class ChatGroupMember
    {
        public int Id { get; set; }
        public int ChatGroupId { get; set; }
        public virtual ChatGroup ChatGroup { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
