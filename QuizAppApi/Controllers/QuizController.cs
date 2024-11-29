using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizAppApi.Data;
using System.Security.Claims;
using QuizAppApi.Models;

namespace QuizAppApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuizController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public QuizController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("{groupId}/quiz")]
        public async Task<IActionResult> GetQuizzesByGroup(Guid groupId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("User Id not found!");

            var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupId == groupId);
            if (group == null) return NotFound();

            var isMember = await _context.GroupMembers
                .Where(gm => (gm.GroupId == group.GroupId && gm.UserId == userId))
                .FirstOrDefaultAsync() != null;
            if (!isMember)
                return Unauthorized("You're not a member of the group!");

            var quizzes = await _context.Quizzes
                .Where(q => (q.GroupId == group.GroupId))
                .Select(q => new
                {
                    q.QuizId,
                    q.Title,
                    q.ScheduledTime,
                    q.Deadline,
                    q.Questions.Count
                })
                .ToListAsync();

            if (quizzes.Count <= 0)
                return NotFound("No quizzes found!");

            return Ok(quizzes);
        }

        [HttpPost("CreateQuiz")]
        public async Task<IActionResult> CreateQuiz([FromForm] Guid groupId, [FromForm] string title, [FromForm] DateTime scheduledtime, [FromForm] DateTime deadline)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return NotFound();

            var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupId == groupId);
            if (group == null) return NotFound("Group not found!");

            if (group.AdminId != userId)
                return Unauthorized("Only the group admin is allowed to perform this action!");

            var quiz = new Quiz()
            {
                QuizId = Guid.NewGuid(),
                Title = title,
                ScheduledTime = scheduledtime,
                Deadline = deadline,
                GroupId = groupId,
                ReportGenerated = false,
                Group = group
            };

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();
            return Ok(quiz);
        }

        [HttpPut("{quizId}/UpdateQuiz")]
        public async Task<IActionResult> UpdateQuiz(Guid quizId, [FromForm] string title, [FromForm] DateTime scheduledtime, [FromForm] DateTime deadline )
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return NotFound();

            var existingQuiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.QuizId == quizId);
            if (existingQuiz == null)
                return NotFound("Quiz not found!");

            var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupId == existingQuiz.GroupId);
            if (group == null)
                return NotFound("No group found for this quiz!");

            if (group.AdminId != userId)
                return BadRequest("You're not authorized to perform this task!");

            existingQuiz.ScheduledTime = scheduledtime;
            existingQuiz.Deadline = deadline;
            existingQuiz.Title = title;

            await _context.SaveChangesAsync();
            return Ok(existingQuiz);
        }

        [HttpDelete("DeleteQuiz/{quizId}")]
        public async Task<IActionResult> DeleteQuiz(Guid quizId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(userId == null)
                return Unauthorized("User Id not found!");

            var quiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.QuizId == quizId);
            if(quiz == null)
                return NotFound("Quiz not found!");

            var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupId == quiz.GroupId);
            if (group == null)
                return NotFound("Group not found!");

            if (group.AdminId != userId)
                return BadRequest("You're not authorized to perform this task!");

            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();
            return Ok("Quiz has been deleted successfully!");
        }

        [HttpPost("{quizId}/AddQuestion")]
        public async Task<IActionResult> AddQuestion(Guid quizId, [FromForm] string text, [FromForm] int difficulty)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("User Id not found!");

            var quiz = _context.Quizzes.FirstOrDefault(q => q.QuizId == quizId);
            if (quiz == null)
                return NotFound("Quiz not found!");

            var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupId == quiz.GroupId);
            if (group == null)
                return NotFound("Group not found!");

            if (group.AdminId != userId) 
                return BadRequest("You're not authorized to perform this task!");

            var question = new Question()
            {
                QuestionId = Guid.NewGuid(),
                Text = text,
                Difficulty = difficulty,
                TimeTaken = 1,
                IsMultipleChoice = true,
                Quiz = quiz
            };
            _context.Questions.Add(question);   
            await _context.SaveChangesAsync();  

            return Ok(question);
        }

        [HttpPut("{questionId}/UpdateQuestion")]
        public async Task<IActionResult> UpdateQuestion(Guid questionId, [FromForm] string text, [FromForm] int difficulty)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("User Id not found!");

            var existingQuestion = await _context.Questions.FirstOrDefaultAsync(q => q.QuestionId == questionId);
            if (existingQuestion == null)
                return NotFound("Question not found!");

            var quiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.QuizId == existingQuestion.QuizId);
            if (quiz == null)
                return NotFound("Quiz not found!");

            var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupId == quiz.GroupId);
            if (group == null)
                return NotFound("Group not found!");

            if (group.AdminId != userId)
                return BadRequest("You're not authorized to perform this task!");

            existingQuestion.Text = text;
            existingQuestion.Difficulty = difficulty;
            await _context.SaveChangesAsync();  
            return Ok(existingQuestion);
        }

        [HttpDelete("{questionId}/DeleteQuestion")]
        public async Task<IActionResult> DeleteQuestion(Guid questionId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("User Id not found!");

            var question = await _context.Questions.FirstOrDefaultAsync(q => q.QuestionId == questionId);
            if (question == null)
                return NotFound("Question not found!");

            var quiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.QuizId == question.QuizId);
            if (quiz == null)
                return NotFound("Quiz not found!");

            var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupId == quiz.GroupId);
            if (group == null)
                return NotFound("Group not found!");

            if (group.AdminId != userId)
                return BadRequest("You're not authorized to perform this task!");

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
            return Ok("Question deleted successfully!");
        }

        [HttpGet("{quizId}/question")]
        public async Task<IActionResult> GetAllQuestionsOfQuiz(Guid quizId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("User Id not found!");

            var quiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.QuizId == quizId);
            if (quiz == null)
                return NotFound("Quiz not found!");

            var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupId == quiz.GroupId);
            if (group == null)
                return NotFound();

            var isMember = await _context.GroupMembers
                .Where(gm => gm.UserId == userId)
                .Include(gm => gm.Group)
                .FirstOrDefaultAsync() != null;

            if (!isMember)
                return Unauthorized("You're not authorized!");

            if (group.AdminId != userId && quiz.ScheduledTime > DateTime.Now)
                return BadRequest("Please don't try to cheat!");

            var questions = await _context.Questions
                .Where(q => q.QuizId == quiz.QuizId)
                .Select(q => new
                {
                    q.QuizId,
                    q.QuestionId,
                    q.Text
                })
                .ToListAsync();

            if (questions.Count <= 0)
                return NotFound("No questions in the quiz!");

            return Ok(questions);
        }

        [HttpPost("{questionId}/AddOption")]
        public async Task<IActionResult> AddOption(Guid questionId, [FromForm] string text, [FromForm] bool iscorrect)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("User Id not found!");

            var question = await _context.Questions.FirstOrDefaultAsync(q => q.QuestionId == questionId);
            if (question == null)
                return NotFound("No question found!");

            var quiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.QuizId == question.QuizId);
            if (quiz == null)
                return NotFound("Quiz not found!");

            var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupId == quiz.GroupId);
            if (group == null)
                return NotFound("Group not found!");

            if (group.AdminId != userId)
                return Unauthorized("You're not authorized to perform this task!");

            var option = new Option
            {
                OptionId = Guid.NewGuid(),
                QuestionId = questionId,
                Text = text,
                IsCorrect = iscorrect,
                Question = question
            };

            _context.Options.Add(option);
            await _context.SaveChangesAsync();
            return Ok(option);
        }

        [HttpPut("{optionId}/UpdateOption")]
        public async Task<IActionResult> UpdateOption(Guid optionId, [FromForm] string text, [FromForm] bool iscorrect)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("User Id not found!");

            var existingOption = await _context.Options.FirstOrDefaultAsync(o => o.OptionId == optionId);
            if (existingOption == null)
                return NotFound("Option not found!");

            var question = await _context.Questions.FirstOrDefaultAsync(q => q.QuestionId == existingOption.QuestionId);
            if (question == null)
                return NotFound("Question does not exist!");

            var quiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.QuizId == question.QuizId);
            if (quiz == null) 
                return NotFound("Quiz not found");

            var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupId == quiz.GroupId);
            if (group == null)
                return NotFound("Group not found!");

            if (group.AdminId != userId)
                return Unauthorized("You're not authorized to perform this task!");

            existingOption.Text = text;
            existingOption.IsCorrect = iscorrect;

            await _context.SaveChangesAsync();
            return Ok(existingOption);
        }

        [HttpDelete("{optionId}/DeleteOption")]
        public async Task<IActionResult> DeleteOption(Guid optionId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("User Id not found!");

            var existingOption = await _context.Options.FirstOrDefaultAsync(o => o.OptionId == optionId);
            if (existingOption == null)
                return NotFound("Option not found!");

            var question = await _context.Questions.FirstOrDefaultAsync(q => q.QuestionId == existingOption.QuestionId);
            if (question == null)
                return NotFound("Question does not exist!");

            var quiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.QuizId == question.QuizId);
            if (quiz == null)
                return NotFound("Quiz not found");

            var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupId == quiz.GroupId);
            if (group == null)
                return NotFound("Group not found!");

            if (group.AdminId != userId)
                return Unauthorized("You're not authorized to perform this task!");
            
            _context.Options.Remove(existingOption);
            await _context.SaveChangesAsync();
            return Ok("Option deleted!");
        }

        [HttpGet("{questionId}/GetOptions")]
        public async Task<IActionResult> GetOptionsOfQuestion(Guid questionId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("User Id not found!");

            var question = _context.Questions.FirstOrDefault(q => q.QuestionId == questionId);
            if (question == null)
                return NotFound("Question not found!");

            var quiz = _context.Quizzes.FirstOrDefault(q => q.QuizId == question.QuizId);
            if (quiz == null)
                return NotFound("Quiz not found!");

            var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupId == quiz.GroupId);
            if (group == null)
                return NotFound("Group not found!");

            if (group.AdminId != userId && quiz.ScheduledTime > DateTime.Now)
                return Unauthorized("No cheating please!");

            var options = await _context.Options
                .Where(o => o.QuestionId == questionId)
                .Select(o => new
                {
                    o.OptionId,
                    o.Text,
                    o.IsCorrect
                })
                .ToListAsync();

            if (options.Count <= 0)
                return NotFound("No options!");

            return Ok(options);
        }
    }
}
