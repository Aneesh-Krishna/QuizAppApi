namespace QuizAppApi.Models
{
    public class QuizResponse
    {
        public Guid QuizResponseId { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public Guid QuizId { get; set; }
        public decimal? Score { get; set; }

        //Navigation Properties
        public ApplicationUser? User { get; set; }
        public Quiz? Quiz { get; set; }
        public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    }
}
