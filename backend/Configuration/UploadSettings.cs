namespace LearnPath.API.Configuration;

public class UploadSettings
{
    public string UploadDirectory { get; set; } = "uploads";
    public int MaxFileSize { get; set; } = 5 * 1024 * 1024;
    public string[] AllowedExtensions { get; set; } = [".pdf", ".doc", ".docx", ".txt"];
    public string[] AllowedMimeTypes { get; set; } =
    [
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "text/plain"
    ];
}
