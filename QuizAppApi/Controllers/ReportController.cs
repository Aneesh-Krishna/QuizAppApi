using Microsoft.AspNetCore.Mvc;
using QuizAppApi.Data;
using QuizAppApi.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace QuizAppApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ReportGenerationService _reportService;

        public ReportController(IServiceScopeFactory scopeFactory, ReportGenerationService reportService)
        {
            _scopeFactory = scopeFactory;
            _reportService = reportService;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateReports()
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
                    await _reportService.GenerateReportsForQuiz(quiz, context);
                    quiz.ReportGenerated = true;
                }

                await context.SaveChangesAsync();
            }

            return Ok("Reports generated and sent successfully!");
        }
    }
}
