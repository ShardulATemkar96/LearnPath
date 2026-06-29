namespace LearnPath.API.DTOs.Certificate;

public class CertificateResponseDto
{
    public int Id { get; set; }
    public int LearningPathId { get; set; }
    public string LearningPathTitle { get; set; } = string.Empty;
    public string CertificateUrl { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
}