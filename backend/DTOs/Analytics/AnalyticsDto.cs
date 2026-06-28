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
