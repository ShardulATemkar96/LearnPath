using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using LearnPath.API.Authentication.Jwt;
using LearnPath.API.Data;
using LearnPath.API.Data.Seeders;
using LearnPath.API.Entities;
using LearnPath.API.Interfaces.Services;
using LearnPath.API.Middleware;
using LearnPath.API.Services.Admin;
using LearnPath.API.Services.Analytics;
using LearnPath.API.Services.Auth;
using LearnPath.API.Services.Classroom;
using LearnPath.API.Services.Dashboard;
using LearnPath.API.Services.LearningPath;
using LearnPath.API.Services.Notification;
using LearnPath.API.Services.Progress;
using LearnPath.API.Services.User;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

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

// ── AutoMapper ────────────────────────────────────────────────
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// ── FluentValidation ──────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LearnPath API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("LearnPathCors");
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
}

app.Run();
