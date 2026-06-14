namespace LearnPath.API.DTOs.Dashboard;

public class DashboardResponseDto
{
    public DashboardStatsDto Stats { get; set; } = null!;
    public List<PathProgressDto> PathProgress { get; set; } = [];
    public List<ActivityItemDto> RecentActivity { get; set; } = [];
}

public class DashboardStatsDto
{
    public int EnrolledPaths { get; set; }
    public int CompletedModules { get; set; }
    public int ActiveClassrooms { get; set; }
    public int Certificates { get; set; }
}

public class PathProgressDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int CompletedModules { get; set; }
    public int TotalModules { get; set; }
}

public class ActivityItemDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
}
