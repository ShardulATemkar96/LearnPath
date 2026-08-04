using LearnPath.API.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LearnPath.API.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<LearningPath> LearningPaths => Set<LearningPath>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<ModuleResource> ModuleResources => Set<ModuleResource>();
    public DbSet<ModuleObjective> ModuleObjectives => Set<ModuleObjective>();
    public DbSet<ModuleTag> ModuleTags => Set<ModuleTag>();
    public DbSet<ModuleDependency> ModuleDependencies => Set<ModuleDependency>();
    public DbSet<QuestionBank> QuestionBanks => Set<QuestionBank>();
    public DbSet<ModuleQuiz> ModuleQuizzes => Set<ModuleQuiz>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Option> Options => Set<Option>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<StudentAnswer> StudentAnswers => Set<StudentAnswer>();
    public DbSet<Progress> Progresses => Set<Progress>();
    public DbSet<Classroom> Classrooms => Set<Classroom>();
    public DbSet<UserClassroom> UserClassrooms => Set<UserClassroom>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<PostVote> PostVotes => Set<PostVote>();
    public DbSet<CommentVote> CommentVotes => Set<CommentVote>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SubmissionAiFeedback> SubmissionAiFeedbacks => Set<SubmissionAiFeedback>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        GuardAuditLogImmutability();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        GuardAuditLogImmutability();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Audit logs are append-only: only Insert and Read are permitted.
    /// Any attempt to update or delete an <see cref="AuditLog"/> is rejected.
    /// </summary>
    private void GuardAuditLogImmutability()
    {
        var violation = ChangeTracker
            .Entries<AuditLog>()
            .Any(e => e.State is EntityState.Modified or EntityState.Deleted);

        if (violation)
            throw new InvalidOperationException(
                "Audit logs are append-only and cannot be modified or deleted.");
    }
}