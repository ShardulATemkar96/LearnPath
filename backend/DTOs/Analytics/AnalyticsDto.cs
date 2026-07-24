namespace LearnPath.API.DTOs.Analytics;

public class UserAnalyticsResponseDto
{
    public int TotalModulesCompleted { get; set; }
    public int TotalPathsEnrolled { get; set; }
    public int TotalCertificates { get; set; }
    public int ActiveClassrooms { get; set; }
    public int CurrentStreak { get; set; }
    public double AverageCompletionRate { get; set; }
    public List<WeeklyActivityDto> WeeklyActivity { get; set; } = [];
    public List<PathCompletionDto> PathCompletions { get; set; } = [];
    public List<ModuleTypeBreakdownDto> ModuleTypeBreakdown { get; set; } = [];
}

public class WeeklyActivityDto
{
    public string Day { get; set; } = string.Empty;
    public int ModulesCompleted { get; set; }
}

public class PathCompletionDto
{
    public string Title { get; set; } = string.Empty;
    public int CompletedModules { get; set; }
    public int TotalModules { get; set; }
    public int Percent { get; set; }
}

public class ModuleTypeBreakdownDto
{
    public string ContentType { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class QuizAnalyticsResponseDto
{
    public int QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public int TotalAttempts { get; set; }
    public int UniqueStudents { get; set; }
    public double AverageScore { get; set; }
    public double PassPercentage { get; set; }
    public int TotalPassed { get; set; }
    public int TotalFailed { get; set; }
    public List<ScoreDistributionDto> ScoreDistribution { get; set; } = [];
    public List<QuestionAnalyticsDto> QuestionAnalytics { get; set; } = [];
    public QuestionAnalyticsDto? MostIncorrectQuestion { get; set; }
    public QuestionAnalyticsDto? HardestQuestion { get; set; }
}

public class ScoreDistributionDto
{
    public string Range { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class QuestionAnalyticsDto
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public int TimesAnswered { get; set; }
    public int TimesCorrect { get; set; }
    public double SuccessRate { get; set; }
    public string Difficulty { get; set; } = string.Empty;
}
