namespace QuizAppApi.Models
{
    public class Quiz
    {
        public Guid QuizId { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public DateTime ScheduledTime { get; set; }
        public DateTime Deadline { get; set; }
        public Guid GroupId { get; set; }

        //Navigation Properties
        public Group? Group { get; set; }
        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<QuizResponse> QuizResponses { get; set; } = new List<QuizResponse>();
        public bool ReportGenerated { get; set; } 
    }
}
