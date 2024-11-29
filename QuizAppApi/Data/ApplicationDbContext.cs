using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuizAppApi.Models;

namespace QuizAppApi.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        //DbSets for entities
        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Option> Options { get; set; }
        public DbSet<QuizResponse> QuizResponses { get; set; }
        public DbSet<Answer> Answers { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            //Group
            builder.Entity<Group>()
                .HasKey(g => g.GroupId); 
            
            builder.Entity<Group>()
                .HasMany(g => g.Members)
                .WithOne(gm => gm.Group)
                .HasForeignKey(gm => gm.GroupId); 
            
            builder.Entity<Group>()
                .HasMany(g => g.Quizzes)
                .WithOne(q => q.Group)
                .HasForeignKey(q => q.GroupId);

            // GroupMember
            builder.Entity<GroupMember>() 
                .HasKey(gm => gm.GroupMemberId); 
            
            builder.Entity<GroupMember>() 
                .HasOne(gm => gm.User) 
                .WithMany(u => u.MemberGroups) 
                .HasForeignKey(gm => gm.UserId) 
                .OnDelete(DeleteBehavior.Cascade); 
            
            builder.Entity<GroupMember>() 
                .HasOne(gm => gm.Group) 
                .WithMany(g => g.Members) 
                .HasForeignKey(gm => gm.GroupId) 
                .OnDelete(DeleteBehavior.Cascade);

            // Quiz
            builder.Entity<Quiz>() 
                .HasKey(q => q.QuizId); 
            
            builder.Entity<Quiz>() 
                .HasMany(q => q.Questions) 
                .WithOne(qt => qt.Quiz) 
                .HasForeignKey(qt => qt.QuizId) 
                .OnDelete(DeleteBehavior.Cascade); 
            
            builder.Entity<Quiz>() 
                .HasMany(q => q.QuizResponses) 
                .WithOne(qr => qr.Quiz) 
                .HasForeignKey(qr => qr.QuizId) 
                .OnDelete(DeleteBehavior.Cascade);

            // Question
            builder.Entity<Question>() 
                .HasKey(q => q.QuestionId); 
            
            builder.Entity<Question>() 
                .HasOne(q => q.Quiz) 
                .WithMany(qu => qu.Questions) 
                .HasForeignKey(q => q.QuizId) 
                .OnDelete(DeleteBehavior.Cascade);

            // Option
            builder.Entity<Option>() 
                .HasKey(o => o.OptionId); 
            
            builder.Entity<Option>() 
                .HasOne(o => o.Question) 
                .WithMany(q => q.Options) 
                .HasForeignKey(o => o.QuestionId) 
                .OnDelete(DeleteBehavior.Cascade);

            // QuizResponse
            builder.Entity<QuizResponse>() 
                .HasKey(qr => qr.QuizResponseId); 
            
            builder.Entity<QuizResponse>() 
                .HasOne(qr => qr.User) 
                .WithMany() 
                .HasForeignKey(qr => qr.UserId) 
                .OnDelete(DeleteBehavior.Cascade); 
            
            builder.Entity<QuizResponse>() 
                .HasOne(qr => qr.Quiz) 
                .WithMany(q => q.QuizResponses) 
                .HasForeignKey(qr => qr.QuizId) 
                .OnDelete(DeleteBehavior.Cascade);

            // Answer
            builder.Entity<Answer>() 
                .HasKey(a => a.AnswerId); 
            
            builder.Entity<Answer>() 
                .HasOne(a => a.QuizResponse) 
                .WithMany(qr => qr.Answers) 
                .HasForeignKey(a => a.ResponseId) 
                .OnDelete(DeleteBehavior.NoAction); 
            
            builder.Entity<Answer>() 
                .HasOne(a => a.Question) 
                .WithMany() 
                .HasForeignKey(a => a.QuestionId) 
                .OnDelete(DeleteBehavior.NoAction); 
            
            builder.Entity<Answer>() 
                .HasOne(a => a.Option) 
                .WithMany() 
                .HasForeignKey(a => a.OptionId) 
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
