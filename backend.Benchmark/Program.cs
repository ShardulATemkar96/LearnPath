// Repeatable performance benchmark for LearnPath learning-path endpoints.
//
// SAFETY:
// - Uses a dedicated database (LearnPathDb_Benchmark) on the local
//   development machine. It HARD-FAILS if the connection target is anything
//   else, and never touches the development database (LearnPathDb).
// - The database is dropped and recreated on every run, so every measurement
//   starts from the same seeded dataset.
//
// Usage: dotnet run --project backend.Benchmark -- --phase baseline
// Output: benchmarks/benchmark-results-<phase>-<timestamp>.json

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using LearnPath.API.Data;
using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace LearnPath.Benchmark;

internal static class Program
{
    private const string ServerName = @"(localdb)\MSSQLLocalDB";
    private const string BenchmarkDatabase = "LearnPathDb_Benchmark";

    private static readonly List<string> StudentIds = [];
    private static readonly List<int> PathIds = [];
    private static string AdminId = "";

    private static async Task<int> Main(string[] args)
    {
        var connectionString =
            $"Server={ServerName};Database={BenchmarkDatabase};" +
            "Trusted_Connection=True;TrustServerCertificate=True;";

        // ── Connection target guard ─────────────────────────────
        var csBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
        if (csBuilder.InitialCatalog != BenchmarkDatabase)
            throw new InvalidOperationException(
                $"Refusing to run: the benchmark must target '{BenchmarkDatabase}', got '{csBuilder.InitialCatalog}'.");
        Console.WriteLine($"[guard] Connection target verified: server='{csBuilder.DataSource}', database='{BenchmarkDatabase}' (isolated benchmark DB).");

        // ── Recreate benchmark database ─────────────────────────
        await using (var bootstrap = CreateContext())
        {
            await bootstrap.Database.EnsureDeletedAsync(); // benchmark DB only
            await bootstrap.Database.MigrateAsync();       // exact production schema
            var currentDb = await bootstrap.Database
                .SqlQuery<string>($"SELECT DB_NAME() AS [Value]")
                .SingleAsync();
            if (currentDb != BenchmarkDatabase)
                throw new InvalidOperationException($"DB_NAME() returned '{currentDb}', expected '{BenchmarkDatabase}'.");
            Console.WriteLine($"[db] Recreated via EF migrations; DB_NAME()={currentDb}");
        }

        // ── Seed realistic, related data ────────────────────────
        var stats = await SeedDataAsync();
        Console.WriteLine(
            $"[seed] paths={stats.Paths}, modules={stats.Modules}, dependencies={stats.Dependencies}, " +
            $"quizzes={stats.Quizzes}, progresses={stats.Progresses}, users={stats.Users} " +
            $"(total={stats.Total} records, seed took {stats.SeedSeconds:F1}s)");

        var studentId = StudentIds[0];           // student with progress in all 20 paths
        var samplePathId = PathIds[0];           // first seeded path (sample for detail reads)
        Console.WriteLine($"[sample] studentId={studentId}, samplePathId={samplePathId}");

        var engineVersion = await GetEngineVersionAsync();

        // ── Measurement ─────────────────────────────────────────
        var phaseArgIdx = Array.FindIndex(args, a => a == "--phase");
        var phase = phaseArgIdx >= 0 && phaseArgIdx + 1 < args.Length
            ? args[phaseArgIdx + 1]
            : "baseline";
        const int WarmupRuns = 3;
        const int MeasuredRuns = 30;

        var results = new List<EndpointResult>();

        var endpoints = new (string Name, Func<RequestScope, Task> Act)[]
        {
            ("GET /paths/public -> GetAllPublicAsync",
                s => s.Paths.GetAllPublicAsync()),
            ("GET /paths/my -> GetMyPathsAsync",
                s => s.Paths.GetMyPathsAsync(AdminId)),
            ("GET /paths/{id} -> GetByIdAsync",
                s => s.Paths.GetByIdAsync(samplePathId, studentId)),
            ("GET /progress -> GetUserProgressAsync",
                s => s.Progress.GetUserProgressAsync(studentId)),
            ("GET /progress/paths/{id} -> GetPathProgressAsync",
                s => s.Progress.GetPathProgressAsync(samplePathId, studentId)),
        };

        foreach (var (name, act) in endpoints)
        {
            for (var i = 0; i < WarmupRuns; i++)
                await WithScopeAsync(act);

            var samples = new List<double>(MeasuredRuns);
            for (var i = 0; i < MeasuredRuns; i++)
            {
                var sw = Stopwatch.StartNew();
                await WithScopeAsync(act);
                sw.Stop();
                samples.Add(sw.Elapsed.TotalMilliseconds);
            }
            samples.Sort();

            var median = Percentile(samples, 0.5);
            var p95 = Percentile(samples, 0.95);
            var queryCount = await CountQueriesAsync(act);

            Console.WriteLine($"[bench] {name,-45} median={median,9:F2} ms  p95={p95,9:F2} ms  queries={queryCount}");
            results.Add(new EndpointResult
            {
                Endpoint = name,
                MedianMs = Math.Round(median, 2),
                P95Ms = Math.Round(p95, 2),
                QueryCount = queryCount,
            });
        }

        // ── Report ──────────────────────────────────────────────
        var report = new BenchmarkReport
        {
            Phase = phase,
            TimestampUtc = DateTime.UtcNow,
            Server = ServerName,
            Database = BenchmarkDatabase,
            EngineVersion = engineVersion.Split('\n')[0].Trim(),
            Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            DatabaseProvider = "Microsoft.EntityFrameworkCore.SqlServer 10.0.9",
            Dataset = stats,
            BenchmarkConfiguration =
                $"fresh DbContext per request (as in production), warmup={WarmupRuns}, " +
                $"measured runs={MeasuredRuns}, connection pooling on, Stopwatch timing",
            Endpoints = results,
        };

        var dir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "benchmarks");
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, $"benchmark-results-{phase}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
        await File.WriteAllTextAsync(
            file,
            JsonSerializer.Serialize(report, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            }));
        Console.WriteLine($"[report] {file}");

        return 0;
    }

    // ═══════════════════ Helpers ═════════════════════════════

    internal static ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(
                $"Server={ServerName};Database={BenchmarkDatabase};" +
                "Trusted_Connection=True;TrustServerCertificate=True;")
            .LogTo(message =>
            {
                if (message.Contains("Executed DbCommand", StringComparison.Ordinal))
                    _ = Interlocked.Increment(ref QueryCounter.Count);
            })
            .Options);

    private static async Task WithScopeAsync(Func<RequestScope, Task> act)
    {
        await using var scope = new RequestScope();
        await act(scope);
    }

    private static async Task<int> CountQueriesAsync(Func<RequestScope, Task> act)
    {
        Interlocked.Exchange(ref QueryCounter.Count, 0);
        await using var scope = new RequestScope();
        await act(scope);
        return Interlocked.Exchange(ref QueryCounter.Count, 0);
    }

    private static async Task<string> GetEngineVersionAsync()
    {
        await using var ctx = CreateContext();
        return await ctx.Database.SqlQuery<string>($"SELECT @@VERSION AS [Value]").FirstAsync();
    }

    private static double Percentile(List<double> sorted, double p)
    {
        if (sorted.Count == 0) return 0;
        var idx = (int)Math.Ceiling(p * sorted.Count) - 1;
        idx = Math.Clamp(idx, 0, sorted.Count - 1);
        return sorted[idx];
    }

    private static string GuidSuffix() => Guid.NewGuid().ToString("N")[..8];

    private static User MakeUser(string email, string firstName, string lastName) =>
        new()
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            Status = UserStatus.Active,
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
        };

    // 2 KB realistic article body — exposes payload overfetching (ContentBody
    // is loaded by list endpoints but excluded from their responses).
    private static string BigBody() =>
        "<article>" + new string('x', 2048) + "</article>";

    private static async Task<SeedStats> SeedDataAsync()
    {
        // 20 paths x 40 modules x 60 students x 20 completed modules = 24k progress rows.
        const int PathCount = 20;
        const int ModulesPerPath = 40;
        const int StudentCount = 60;

        var sw = Stopwatch.StartNew();
        await using var ctx = CreateContext();
        var rnd = new Random(42); // deterministic dataset

        var admin = MakeUser("benchmark-admin@learnpath.dev", "System", "Admin");
        AdminId = admin.Id;
        var students = new List<User>(StudentCount);
        for (var s = 0; s < StudentCount; s++)
        {
            var student = MakeUser(
                $"benchmark-student-{s}-{GuidSuffix()}@learnpath.dev",
                "Student", GuidSuffix().ToUpperInvariant());
            students.Add(student);
            StudentIds.Add(student.Id);
        }
        ctx.Users.Add(admin);
        ctx.Users.AddRange(students);
        await ctx.SaveChangesAsync();

        var paths = 0;
        var modules = 0;
        var dependencies = 0;
        var quizzes = 0;
        var progresses = 0;

        for (var p = 0; p < PathCount; p++)
        {
            var path = new LearningPath
            {
                Title = $"Benchmark Path {p}",
                Description =
                    "A realistic benchmark learning path covering the full journey from " +
                    "fundamentals to advanced topics, with prerequisite chains that a student " +
                    "traverses module by module. " + new string('y', 300),
                IsPublished = true,
                IsPublic = true,
                CreatedById = admin.Id,
            };
            ctx.LearningPaths.Add(path);
            await ctx.SaveChangesAsync();
            paths++;
            PathIds.Add(path.Id);

            var moduleList = new List<Module>(ModulesPerPath);
            for (var i = 0; i < ModulesPerPath; i++)
            {
                moduleList.Add(new Module
                {
                    Title = $"Benchmark Path {p} Module {i + 1}",
                    Description = $"Benchmark module {i + 1} of path {p}.",
                    ContentType = i % 3 == 0 ? "article" : i % 3 == 1 ? "video" : "quiz",
                    Order = i + 1,
                    LearningPathId = path.Id,
                    // 8 of 40 unpublished — mirrors mixed draft state in dev data.
                    IsPublished = i % 5 != 4,
                    IsDraft = i % 5 == 4,
                    ContentUrl = $"https://cdn.learnpath.local/bench/{p}/{i}.mp4",
                    ContentBody = BigBody(),
                    Difficulty = ModuleDifficulty.Beginner,
                });
            }
            ctx.Modules.AddRange(moduleList);
            await ctx.SaveChangesAsync();
            modules += moduleList.Count;

            // Dependency chain 2->1, 3->2, ... plus a few side branches to make
            // the DAG realistically non-linear.
            var deps = new List<ModuleDependency>(ModulesPerPath);
            for (var i = 1; i < ModulesPerPath; i++)
                deps.Add(new ModuleDependency
                { ModuleId = moduleList[i].Id, DependsOnModuleId = moduleList[i - 1].Id });
            for (var i = 8; i < ModulesPerPath; i++)
                if (i % 7 == 0)
                    deps.Add(new ModuleDependency
                    { ModuleId = moduleList[i].Id, DependsOnModuleId = moduleList[i - 5].Id });
            ctx.ModuleDependencies.AddRange(deps);
            await ctx.SaveChangesAsync();
            dependencies += deps.Count;

            // One published quiz attached to the last published module of the path.
            var questionBank = new QuestionBank
            {
                Title = $"Benchmark Question Bank {p}",
                Subject = "Benchmark",
                OriginalFileName = "benchmark.json",
                StoredJson = "{}",
                QuestionCount = 5,
                CreatedBy = "benchmark-admin@learnpath.dev",
            };
            var quiz = new Quiz
            {
                Title = $"Benchmark Quiz {p}",
                QuestionBank = questionBank,
                QuestionCount = 5,
                SelectionMode = SelectionMode.Random,
                TimeLimitMinutes = 30,
                PassingPercentage = 40,
                MaximumAttempts = 3,
                Status = QuizStatus.Published,
                CreatedById = admin.Id,
            };
            var quizModule = moduleList.Last(m => m.IsPublished);
            ctx.Quizzes.Add(quiz);
            ctx.ModuleQuizzes.Add(new ModuleQuiz
            {
                ModuleId = quizModule.Id,
                Quiz = quiz, // navigation assignment propagates the new quiz id
                AssignedBy = admin.Id,
                AssignedAt = DateTime.UtcNow,
                Active = true,
            });
            await ctx.SaveChangesAsync();
            quizzes++;

            // Every student completes the first 24 published modules of the path.
            var completedModuleIds = moduleList
                .Where(m => m.Order <= 25 && m.IsPublished)
                .OrderBy(m => m.Order)
                .Select(m => m.Id)
                .ToList();
            var batch = new List<Progress>(StudentCount * completedModuleIds.Count / 4);
            foreach (var student in students)
            {
                foreach (var moduleId in completedModuleIds)
                {
                    batch.Add(new Progress
                    {
                        UserId = student.Id,
                        ModuleId = moduleId,
                        IsCompleted = true,
                        CompletedAt = DateTime.UtcNow.AddDays(-rnd.Next(1, 30)),
                    });
                    if (batch.Count < 2000) continue;
                    ctx.Progresses.AddRange(batch);
                    await ctx.SaveChangesAsync();
                    progresses += batch.Count;
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                ctx.Progresses.AddRange(batch);
                await ctx.SaveChangesAsync();
                progresses += batch.Count;
                batch.Clear();
            }
        }

        return new SeedStats(
            Users: 1 + StudentCount,
            Paths: paths,
            Modules: modules,
            Dependencies: dependencies,
            Quizzes: quizzes,
            Progresses: progresses,
            SeedSeconds: sw.Elapsed.TotalSeconds);
    }
}

// ═══════════════════════ Support types ═══════════════════════════

internal static class QueryCounter
{
    public static int Count;
}

internal record SeedStats(
    int Users, int Paths, int Modules, int Dependencies, int Quizzes,
    int Progresses, double SeedSeconds)
{
    public int Total => Users + Paths + Modules + Dependencies + Quizzes + Progresses;
}

internal sealed class EndpointResult
{
    public string Endpoint { get; init; } = "";
    public double MedianMs { get; init; }
    public double P95Ms { get; init; }
    public int QueryCount { get; init; }
}

internal sealed class BenchmarkReport
{
    public string Phase { get; init; } = "";
    public DateTime TimestampUtc { get; init; }
    public string Server { get; init; } = "";
    public string Database { get; init; } = "";
    public string EngineVersion { get; init; } = "";
    public string Framework { get; init; } = "";
    public string DatabaseProvider { get; init; } = "";
    public SeedStats Dataset { get; init; } = new(0, 0, 0, 0, 0, 0, 0);
    public string BenchmarkConfiguration { get; init; } = "";
    public List<EndpointResult> Endpoints { get; init; } = [];
}

internal sealed class RequestScope : IAsyncDisposable
{
    public ApplicationDbContext Context { get; }
    public LearnPath.API.Services.LearningPath.LearningPathService Paths { get; }
    public LearnPath.API.Services.Progress.ProgressService Progress { get; }

    public RequestScope()
    {
        Context = Program.CreateContext();
        Paths = new(
            Context, new NoopAuditLog(),
            NullLogger<LearnPath.API.Services.LearningPath.LearningPathService>.Instance);
        Progress = new(Context, new NoopAuditLog());
    }

    public ValueTask DisposeAsync() => Context.DisposeAsync();
}

/// <summary>
/// Audit log sink that records nothing: the benchmark reads must not pay the
/// cost of appending audit rows (production writes audit on mutations, not
/// on the read endpoints measured here).
/// </summary>
internal sealed class NoopAuditLog : LearnPath.API.Interfaces.Services.IAuditLogService
{
    public Task<LearnPath.API.DTOs.Audit.AuditLogListResponseDto> GetMyHistoryAsync(
        string userId, LearnPath.API.DTOs.Audit.AuditLogQueryDto query) =>
        throw new NotSupportedException("Benchmark never queries audit history.");

    public List<LearnPath.API.DTOs.Audit.AuditActionOptionDto> GetActionOptions() => [];

    public Task<LearnPath.API.DTOs.Audit.AdminAuditLogListResponseDto> GetAdminHistoryAsync(
        string requestingUserId, LearnPath.API.DTOs.Audit.AdminAuditLogQueryDto query) =>
        throw new NotSupportedException("Benchmark never queries audit history.");

    public Task<bool> CanAccessAdminHistoryAsync(string requestingUserId) =>
        Task.FromResult(false);

    public Task<List<string>> GetEntityTypesAsync(string requestingUserId) =>
        Task.FromResult(new List<string>());

    public Task LogAsync(
        LearnPath.API.Entities.AuditAction actionType, string entityType, string entityId,
        string description, string? oldValue = null, string? newValue = null,
        string? additionalData = null, string? userId = null, string? username = null,
        string? role = null) =>
        Task.CompletedTask;
}


