using System.Text;
using System.Text.Json.Serialization;
using FluentValidation;
using LearnPath.API.Authentication.Jwt;
using LearnPath.API.Common;
using LearnPath.API.Configuration;
using LearnPath.API.Data;
using LearnPath.API.Data.Seeders;
using LearnPath.API.Entities;
using LearnPath.API.Interfaces.Repositories;
using LearnPath.API.Interfaces.Services;
using LearnPath.API.Middleware;
using LearnPath.API.Services.Admin;
using LearnPath.API.Services.Analytics;
using LearnPath.API.Services.Attempt;
using LearnPath.API.Services.Audit;
using LearnPath.API.Services.Community;
using LearnPath.API.Services.Auth;
using LearnPath.API.Services.Classroom;
using LearnPath.API.Services.Dashboard;
using LearnPath.API.Services.LearningPath;
using LearnPath.API.Services.Notification;
using LearnPath.API.Services.Progress;
using LearnPath.API.Services.QuestionBank;
using LearnPath.API.Services.Quiz;
using LearnPath.API.Services.Submission;
using LearnPath.API.Services.Validation;
using LearnPath.API.Services.User;
using LearnPath.API.Interfaces.Services.Ai;
using LearnPath.API.Services.Ai;
using LearnPath.API.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Load .env file ────────────────────────────────────────────
var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
Dictionary<string, string?> envVars = new();

if (File.Exists(envPath))
{
    envVars = DotNetEnv.Env.Load(envPath)
        .GroupBy(kvp => kvp.Key)
        .ToDictionary(g => g.Key, g => (string?)g.First().Value);
}

// ── Secure Configuration ─────────────────────────────────────
// Map environment variables to configuration keys.
// These override values in appsettings.json and are never committed.
var envMapping = new Dictionary<string, string?>
{
    ["AiOptions:ApiKey"] = GetEnv("LEARNPATH_NVIDIA_API_KEY"),
    ["AiOptions:Model"] = GetEnv("LEARNPATH_NVIDIA_MODEL"),
    ["AiOptions:BaseUrl"] = GetEnv("LEARNPATH_NVIDIA_BASE_URL"),
    ["JwtSettings:Secret"] = GetEnv("LEARNPATH_JWT_SECRET"),
    ["ConnectionStrings:DefaultConnection"] = GetEnv("LEARNPATH_DB_CONNECTION"),
};

var validMapping = envMapping
    .Where(kvp => kvp.Value is not null)
    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

builder.Configuration.AddInMemoryCollection(validMapping!);

// Diagnostic: verify configuration loaded correctly (no values exposed)
var cfgApiKey = builder.Configuration["AiOptions:ApiKey"];
var cfgModel  = builder.Configuration["AiOptions:Model"];
var cfgJwt    = builder.Configuration["JwtSettings:Secret"];
var cfgDb     = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"[Config] AI API Key: {(cfgApiKey is { Length: > 0 } ? "✓ loaded" : "✗ MISSING")}");
Console.WriteLine($"[Config] AI Model:   {(cfgModel is { Length: > 0 } ? "✓ loaded" : "✗ MISSING")}");
Console.WriteLine($"[Config] JWT Secret: {(cfgJwt is { Length: > 0 } ? "✓ loaded" : "✗ MISSING")}");
Console.WriteLine($"[Config] DB ConStr:  {(cfgDb is { Length: > 0 } ? "✓ loaded" : "✗ MISSING")}");

// Validate required environment variables are set.
var missingVars = new List<string>();

if (string.IsNullOrWhiteSpace(GetEnv("LEARNPATH_NVIDIA_API_KEY")))
    missingVars.Add("LEARNPATH_NVIDIA_API_KEY (NVIDIA API key for AI feedback)");

if (string.IsNullOrWhiteSpace(GetEnv("LEARNPATH_JWT_SECRET")))
    missingVars.Add("LEARNPATH_JWT_SECRET (JWT signing secret)");

if (string.IsNullOrWhiteSpace(GetEnv("LEARNPATH_DB_CONNECTION")))
    missingVars.Add("LEARNPATH_DB_CONNECTION (SQL Server database connection string)");

if (missingVars.Count > 0)
{
    var message = "The following required environment variables are not set:\n"
        + string.Join("\n", missingVars.Select(m => $"  - {m}"))
        + "\n\nCreate a .env file in the backend/ directory with these values."
        + "\nSee .env.example for the template.";
    throw new InvalidOperationException(message);
}

// Helper: read from envVars dictionary first, fall back to process env vars
string? GetEnv(string key) => envVars.GetValueOrDefault(key)
    ?? Environment.GetEnvironmentVariable(key);

// ── Database ──────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure()));

// ── Identity ──────────────────────────────────────────────────
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit            = true;
    options.Password.RequireLowercase        = true;
    options.Password.RequireUppercase        = true;
    options.Password.RequiredLength          = 8;
    options.Password.RequireNonAlphanumeric  = false;
    options.User.RequireUniqueEmail          = true;
    options.Lockout.DefaultLockoutTimeSpan   = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts  = 5;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ── JWT ───────────────────────────────────────────────────────
var jwtSection = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSection);
builder.Services.AddSingleton<JwtTokenGenerator>();

var jwtSettings = jwtSection.Get<JwtSettings>()!;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = jwtSettings.Issuer,
        ValidAudience            = jwtSettings.Audience,
        IssuerSigningKey         = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        ClockSkew                = TimeSpan.Zero,
    };
});

// ── Authorization ─────────────────────────────────────────────
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", p => p.RequireRole("Admin"))
    .AddPolicy("InstructorOrAdmin", p => p.RequireRole("Admin", "Instructor"));

// ── CORS ──────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("LearnPathCors", policy =>
    {
        policy
            .WithOrigins(
                builder.Configuration["AllowedOrigins"]!.Split(","))
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
/**/
// ── Upload Configuration ──────────────────────────────────────
builder.Services.Configure<UploadSettings>(
    builder.Configuration.GetSection("UploadSettings"));

// ── AutoMapper ────────────────────────────────────────────────
builder.Services.AddAutoMapper(cfg => { }, typeof(Program).Assembly);
// ── FluentValidation ──────────────────────────────────────────
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // ModelState validation failures are converted to the same ApiResponse
        // envelope used everywhere else, so the frontend always receives a
        // human-readable message instead of ASP.NET's raw ValidationProblemDetails.
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(kv => kv.Value?.Errors.Count > 0)
                .SelectMany(kv => kv.Value!.Errors.Select(e =>
                    string.IsNullOrWhiteSpace(e.ErrorMessage)
                        ? $"{kv.Key}: {e.Exception?.Message}"
                        : e.ErrorMessage))
                .ToList();

            var message = errors.Count > 0
                ? string.Join(" ", errors)
                : "Validation failed.";

            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(
                ApiResponse<object>.Fail(message, errors));
        };
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ── Services ──────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService,         AuthService>();
builder.Services.AddScoped<IDashboardService,    DashboardService>();
builder.Services.AddScoped<ILearningPathService, LearningPathService>();
builder.Services.AddScoped<IClassroomService,    ClassroomService>();
builder.Services.AddScoped<IProgressService,     ProgressService>();
builder.Services.AddScoped<IAnalyticsService,    AnalyticsService>();
builder.Services.AddScoped<IUserService,         UserService>();
builder.Services.AddScoped<IAdminService,        AdminService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ICommunityService, CommunityService>();
builder.Services.AddScoped<IValidationService, JsonValidationService>();
builder.Services.AddScoped<IQuestionBankService, QuestionBankService>();
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<IAttemptService, AttemptService>();
builder.Services.AddScoped<IFileValidationService, FileValidationService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddHttpContextAccessor();

// ── Repositories ──────────────────────────────────────────────
builder.Services.AddScoped<ICommunityRepository, CommunityRepository>();

// ── AI Services ────────────────────────────────────────────────
builder.Services.Configure<AiOptions>(
    builder.Configuration.GetSection("AiOptions"));
builder.Services.AddHttpClient<IAiProvider, NvidiaProvider>();
builder.Services.AddScoped<IAiFeedbackService, AiFeedbackService>();
builder.Services.AddScoped<PromptBuilder>();
builder.Services.AddScoped<AiResponseParser>();

// ── Swagger ───────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "LearnPath API",
        Version     = "v1",
        Description = "Graph-based personalized learning platform API",
    });

    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter JWT token",
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer",
                }
            },
            Array.Empty<string>()
        }
    });
});

// ── Build ─────────────────────────────────────────────────────
var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────
app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<LoggingMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LearnPath API v1");
        c.RoutePrefix = "swagger";
    });
}

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection(); app.UseCors("LearnPathCors");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Seed ──────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();
    await RoleSeeder.SeedAsync(roleManager);
    await AdminSeeder.SeedAsync(scope.ServiceProvider);
}
app.Run();
