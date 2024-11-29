namespace QuizAppApi.Models
{
    public class Group
    {
        public Guid GroupId { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string? AdminId { get; set; } = string.Empty;

        //Navigation Properties
        public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
        public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    }
}
