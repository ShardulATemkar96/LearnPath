using LearnPath.API.Configuration;
using LearnPath.API.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace LearnPath.API.Services.Submission;

public class FileValidationService : IFileValidationService
{
    private readonly UploadSettings _settings;

    private static readonly Dictionary<string, string> ExtensionMimeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".pdf", "application/pdf" },
        { ".doc", "application/msword" },
        { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { ".txt", "text/plain" },
    };

    public FileValidationService(IOptions<UploadSettings> settings)
    {
        _settings = settings.Value;
    }

    public FileValidationResult Validate(IFormFile file)
    {
        if (file.Length > _settings.MaxFileSize)
        {
            return new FileValidationResult
            {
                IsValid = false,
                ErrorMessage = $"File exceeds maximum size of {_settings.MaxFileSize / (1024 * 1024)} MB."
            };
        }

        if (file.Length == 0)
        {
            return new FileValidationResult
            {
                IsValid = false,
                ErrorMessage = "Uploaded file is empty."
            };
        }

        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !_settings.AllowedExtensions.Contains(ext))
        {
            return new FileValidationResult
            {
                IsValid = false,
                ErrorMessage = $"File type '{ext}' is not supported. Accepted types: {string.Join(", ", _settings.AllowedExtensions)}."
            };
        }

        var mime = file.ContentType?.ToLowerInvariant();
        if (string.IsNullOrEmpty(mime) || !_settings.AllowedMimeTypes.Contains(mime))
        {
            return new FileValidationResult
            {
                IsValid = false,
                ErrorMessage = $"MIME type '{mime}' is not accepted."
            };
        }

        if (ExtensionMimeMap.TryGetValue(ext, out var expectedMime) && mime != expectedMime)
        {
            return new FileValidationResult
            {
                IsValid = false,
                ErrorMessage = $"MIME type '{mime}' does not match the file extension '{ext}'."
            };
        }

        return new FileValidationResult
        {
            IsValid = true,
            Extension = ext.TrimStart('.'),
            MimeType = mime
        };
    }
}
