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
    public DbSet<ModuleDependency> ModuleDependencies => Set<ModuleDependency>();
    public DbSet<Progress> Progresses => Set<Progress>();
    public DbSet<Classroom> Classrooms => Set<Classroom>();
    public DbSet<UserClassroom> UserClassrooms => Set<UserClassroom>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}