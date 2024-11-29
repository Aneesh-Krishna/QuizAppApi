using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using iTextSharp.text;
using iTextSharp.text.pdf;
using QuizAppApi.Data;
using QuizAppApi.Models;
using Microsoft.ML;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR;
using QuizAppApi.Hubs;

namespace QuizAppApi.Services
{
    public class ReportGenerationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<ReportHub> _hubContext;

        public ReportGenerationService(IServiceScopeFactory scopeFactory, IHubContext<ReportHub> hubContext)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var now = DateTime.Now;
                    var quizzes = await context.Quizzes
                        .Where(q => q.Deadline.AddMinutes(1) <= now && !q.ReportGenerated)
                        .ToListAsync();

                    foreach (var quiz in quizzes)
                    {
                        await GenerateReportsForQuiz(quiz, context);
                        quiz.ReportGenerated = true;
                    }

                    await context.SaveChangesAsync();
                }
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Runs every minute
            }
        }

        public async Task GenerateReportsForQuiz(Quiz quiz, ApplicationDbContext context)
        {
            // Generate Normal Report
            await GenerateNormalReport(quiz, context);

            // Generate Analysis Report
        }

        private async Task GenerateNormalReport(Quiz quiz, ApplicationDbContext context)
        {
            // Fetch group and quiz data
            var group = await context.Groups
                .Include(g => g.Members)
                .ThenInclude(gm => gm.User)
                .FirstOrDefaultAsync(g => g.GroupId == quiz.GroupId);
            if (group == null)
            {
                Console.WriteLine("Group not found!");
                return;
            }

            using (var memoryStream = new MemoryStream())
            {
                var document = new Document();
                PdfWriter.GetInstance(document, memoryStream).CloseStream = false;
                document.Open();
                document.Add(new Paragraph("Quiz Scores Report"));

                var Members = await context.GroupMembers.Where(gm => gm.GroupId == group.GroupId).ToListAsync();
                if (Members == null)
                {
                    Console.WriteLine("No group members!");
                    return;
                }

                foreach (var member in Members)
                {
                    var user = await context.Users.FindAsync(member.UserId);
                    if (user == null)
                    { 
                        continue; 
                    }
                    var response = quiz.QuizResponses.FirstOrDefault(qr => qr.UserId == member.UserId);
                    document.Add(new Paragraph($"User: {user.FullName}, Score: {response?.Score ?? 0}, Status: {(response == null ? "Absent" : "Present")}"));
                }

                document.Close();
                memoryStream.Position = 0;

                // Ensure the directory exists
                var reportDirectory = Path.Combine("Reports"); 
                if (!Directory.Exists(reportDirectory)) 
                { 
                    Directory.CreateDirectory(reportDirectory); 
                }

                // Save the PDF report
                var reportPath = Path.Combine(reportDirectory, $"{quiz.Title}_Report.pdf");
                await File.WriteAllBytesAsync(reportPath, memoryStream.ToArray());

                // Notify the group via SignalR
                try
                {
                    await _hubContext.Clients.Group(group.GroupId.ToString()).SendAsync("ReceiveReport", reportPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending SignalR message: {ex.Message}");
                }
            }
        }

        private async Task GenerateAnalysisReport(Quiz quiz, ApplicationDbContext context)
        {
            var modelPath = "quizModel.zip";

            if (!File.Exists(modelPath))
            {
                Console.WriteLine("Analysis model not found. Training a new model...");
                TrainAndSaveModel(modelPath);
                Console.WriteLine($"Model trained and saved to {modelPath}");
            }

            var mlContext = new MLContext();
            var model = mlContext.Model.Load(modelPath, out var schema);
            var predictionEngine = mlContext.Model.CreatePredictionEngine<QuizData, QuizPrediction>(model);

            var averageDifficulty = quiz.Questions.Average(q => q.Difficulty);
            var totalTimeTaken = quiz.Questions.Sum(q => q.TimeTaken);

            var prediction = predictionEngine.Predict(new QuizData
            {
                QuestionDifficulty = averageDifficulty,
                TimeTaken = totalTimeTaken
            });

            using (var memoryStream = new MemoryStream())
            {
                var document = new Document();
                PdfWriter.GetInstance(document, memoryStream).CloseStream = false;
                document.Open();
                document.Add(new Paragraph("Quiz Analysis Report"));
                document.Add(new Paragraph($"Predicted score for the quiz: {prediction.Score}"));

                document.Close();
                memoryStream.Position = 0;

                // Ensure the directory exists
                var reportDirectory = Path.Combine("Reports"); 
                if (!Directory.Exists(reportDirectory)) 
                { 
                    Directory.CreateDirectory(reportDirectory); 
                }

                // Save the PDF report
                var analysisReportPath = Path.Combine(reportDirectory, $"{quiz.Title}_AnalysisReport.pdf");
                await File.WriteAllBytesAsync(analysisReportPath, memoryStream.ToArray());

                // Notify the group via SignalR
                try
                {
                    await _hubContext.Clients.Group(quiz.GroupId.ToString()).SendAsync("ReceiveAnalysisReport", analysisReportPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending SignalR message: {ex.Message}");
                }
            }
        }
        private void TrainAndSaveModel(string modelPath)
        {
            var mlContext = new MLContext();

            // Sample data for training
            var trainingData = new[]
            {
                new QuizData { QuestionDifficulty = 2.5, TimeTaken = 10, Score = 80 },
                new QuizData { QuestionDifficulty = 3.0, TimeTaken = 12, Score = 70 },
                new QuizData { QuestionDifficulty = 4.0, TimeTaken = 15, Score = 60 },
                new QuizData { QuestionDifficulty = 1.5, TimeTaken = 8, Score = 90 }
            };

            var data = mlContext.Data.LoadFromEnumerable(trainingData);

            // Define the data process pipeline
            var pipeline = mlContext.Transforms.Concatenate("Features", nameof(QuizData.QuestionDifficulty), nameof(QuizData.TimeTaken))
                .Append(mlContext.Regression.Trainers.Sdca(labelColumnName: "Score", maximumNumberOfIterations: 100));

            // Train the model
            var model = pipeline.Fit(data);

            // Save the model
            mlContext.Model.Save(model, data.Schema, modelPath);
        }
    }

    public class QuizData
    {
        public double QuestionDifficulty { get; set; }
        public double TimeTaken { get; set; }
        public double Score { get; set; }
    }

    public class QuizPrediction
    {
        public double Score { get; set; }
    }
}
