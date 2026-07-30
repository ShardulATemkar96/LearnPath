using Microsoft.AspNetCore.Http;

namespace LearnPath.API.Interfaces.Services;

public class FileValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Extension { get; set; }
    public string? MimeType { get; set; }
}

public interface IFileValidationService
{
    FileValidationResult Validate(IFormFile file);
}
