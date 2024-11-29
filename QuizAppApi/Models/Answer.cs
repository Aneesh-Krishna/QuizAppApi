namespace QuizAppApi.Models
{
    public class Answer
    {
        public Guid AnswerId { get; set; } = Guid.NewGuid();
        public Guid ResponseId { get; set; }
        public Guid QuestionId { get; set; }
        public Guid? OptionId { get; set; } //Nullable for questions where answer is typable without any options

        //Navigation Properties
        public QuizResponse QuizResponse { get; set; } = null!;
        public Question Question { get; set; } = null!;
        public Option? Option { get; set; }

    }
}
