namespace LearnPath.API.DTOs.Community;

public class CreateReportDto
{
    public string Reason { get; set; } = string.Empty;
}

public class ReportDto
{
    public int Id { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public int TargetId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ReportedByUserId { get; set; } = string.Empty;
    public string ReportedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ReportListResponseDto
{
    public List<ReportDto> Reports { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
