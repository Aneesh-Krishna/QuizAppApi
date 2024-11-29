using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizAppApi.Data;
using System.Security.Claims;
using QuizAppApi.Models;

namespace QuizAppApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuizResponseController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public QuizResponseController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("{quizId}/responses")]
        public async Task<IActionResult> AllQuizResponses(Guid quizId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("User Id not found!");

            var quiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.QuizId == quizId);
            if (quiz == null) return NotFound("Quiz not found!");

            if (quiz.Group.AdminId != userId)
                return Unauthorized("You're not authorized to view the details!");

            var quizResponses = await _context.QuizResponses
                .Where(q => q.QuizId == quizId)
                .ToListAsync();

            if (quizResponses == null)
                return NotFound("No responses for this quiz!");

            return Ok(quizResponses);
        }

        [HttpGet("{userid}/GetQuizResponse")]
        public async Task<IActionResult> QuizResponsesOfUser(string userid, Guid groupId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("User Id no found!");

            var group = await _context.Groups.FirstOrDefaultAsync(q => q.GroupId == groupId);
            if (group == null) return NotFound("Group not found!");

            if (group.AdminId != userId || userid != userId)
                return Unauthorized("You're not authorized to view the details!");

            var quizzesOfTheGroup = await _context.Quizzes
                .Where(q => q.GroupId == groupId)
                .ToListAsync();
            if (quizzesOfTheGroup == null || !quizzesOfTheGroup.Any())
                return NotFound("No quizzes in the group!");

            var quizIds = quizzesOfTheGroup.Select(q => q.QuizId).ToList();

            var quizResponses = await _context.QuizResponses
                .Where(qr => (qr.UserId == userid && quizIds.Contains(qr.QuizId)))
                .ToListAsync();
            if (quizResponses == null) return NotFound("No responses!");

            return Ok(quizResponses);
        }

        [HttpPost("{quizId}/Submit")]
        public async Task<IActionResult> SubmitAnswers(Guid quizId, [FromBody] SubmitQuizModel model)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return NotFound("User Id not found!");

            var quiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.QuizId == quizId);
            if (quiz == null) return NotFound("Quiz not found!");

            if (DateTime.Now > quiz.Deadline)
                return BadRequest("The quiz's deadline has passed!");

            var existingQuizResponse = await _context.QuizResponses.FirstOrDefaultAsync(qr => qr.QuizId == quizId && qr.UserId == userId); 
            
            if (existingQuizResponse != null) 
                return BadRequest("You have already submitted responses for this quiz.");

            List<Answer> FinalAnswers = new List<Answer>();

            var quizResponse = new QuizResponse
            {
                QuizResponseId = Guid.NewGuid(),
                QuizId = quizId,
                UserId = userId,
                User = await _context.Users.FindAsync(userId),
                Quiz = quiz
            };

            int finalScore = 0;

            foreach (var answer in model.Answers)
            {
                var question = await _context.Questions.FirstOrDefaultAsync(q => q.QuestionId == answer.questionid);
                if (question == null)
                    return NotFound($"Question {answer.questionid} not found!");
                
                var option = await _context.Options.FirstOrDefaultAsync(o => (o.OptionId == answer.optionid && o.QuestionId == answer.questionid));
                if (option == null)
                    return NotFound("Select valid option!");

                if(option.IsCorrect)
                    finalScore++;

                var finalanswer = new Answer
                {
                    AnswerId = Guid.NewGuid(),
                    ResponseId = quizResponse.QuizResponseId,
                    QuestionId = answer.questionid,
                    OptionId = answer.optionid,
                    QuizResponse = quizResponse,
                    Question = question,
                    Option = option
                };

                FinalAnswers.Add(finalanswer);
            }

            using var transaction = await _context.Database.BeginTransactionAsync(); 
            try 
            { 
                _context.QuizResponses.Add(quizResponse); 
                await _context.SaveChangesAsync(); 
                _context.Answers.AddRange(FinalAnswers); 
                await _context.SaveChangesAsync(); 
                await transaction.CommitAsync(); 
            } 
            catch (Exception ex) 
            { 
                await transaction.RollbackAsync(); 
                return StatusCode(500, "An error occurred while saving the data. Please try again."); 
            }

            //Fetch correct options
            var correctOptions = await _context.Options
                .Where(o => FinalAnswers.Select(a => a.QuestionId).Contains(o.QuestionId) && o.IsCorrect)
                .ToListAsync();

            int score = 0;
            foreach (var answer in FinalAnswers)
            {
                if (correctOptions.Any(o => o.QuestionId == answer.QuestionId && o.OptionId == answer.OptionId))
                {
                    score++;
                }
            }

            quizResponse.Score = finalScore;

            // Calculate and save the time taken for the quiz
            foreach (var answer in FinalAnswers)
            {
                var question = await _context.Questions.FirstOrDefaultAsync(q => q.QuestionId == answer.QuestionId);
                if (question != null)
                {
                    question.TimeTaken = model.TimeTaken; // Assuming TimeTaken is passed in the model
                }
            }

            await _context.SaveChangesAsync();

            return Ok("Answers submitted successfully!");
        }
    }

    public class SubmitQuizModel
    {
        public List<AnswerModelForSubmission> Answers { get; set; } = new List<AnswerModelForSubmission>();
        public int TimeTaken { get; set; } // Add this property
    }

    public class AnswerModelForSubmission
    {
        public Guid questionid { get; set; }
        public Guid optionid { get; set; }
    }
}
