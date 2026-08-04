namespace LearnPath.API.DTOs.Certificate;

public class CertificateResponseDto
{
    public int Id { get; set; }
    public int LearningPathId { get; set; }
    public string LearningPathTitle { get; set; } = string.Empty;
    public string CertificateUrl { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
}

public class AdminCertificateResponseDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public int LearningPathId { get; set; }
    public string LearningPathTitle { get; set; } = string.Empty;
    public string CertificateNumber { get; set; } = string.Empty;
    public string CertificateUrl { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class AdminCertificateListResponseDto
{
    public List<AdminCertificateResponseDto> Entries { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}