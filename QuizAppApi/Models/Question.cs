namespace QuizAppApi.Models
{
    public class Question
    {
        public Guid QuestionId { get; set; } = Guid.NewGuid();
        public string Text { get; set; } = string.Empty;
        public bool IsMultipleChoice { get; set; }
        public Guid QuizId { get; set; }
        public int Difficulty { get; set; }
        public int TimeTaken { get; set; }

        //Navigation Properties
        public Quiz? Quiz { get; set; }
        public ICollection<Option> Options { get; set; } = new List<Option>();
    }
}
