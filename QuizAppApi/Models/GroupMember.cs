namespace QuizAppApi.Models
{
    public class GroupMember
    {
        public Guid GroupMemberId { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public Guid GroupId { get; set; }

        //Navigation Properties
        public ApplicationUser? User { get; set; }
        public Group? Group { get; set; }
    }
}
