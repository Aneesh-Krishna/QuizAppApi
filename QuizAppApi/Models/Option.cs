namespace QuizAppApi.Models
{
    public class Option
    {
        public Guid OptionId { get; set; } = Guid.NewGuid();
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public Guid QuestionId { get; set; }

        //Navigation Properties
        public Question? Question { get; set; }
    }
}
