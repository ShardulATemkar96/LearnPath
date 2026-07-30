namespace LearnPath.API.Interfaces.Services;

public class FileStorageResult
{
    public bool Success { get; set; }
    public string? StoredFilePath { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IFileStorageService
{
    Task<FileStorageResult> StoreAsync(int assignmentId, string userId, Stream fileStream, string extension);
    Task DeleteAsync(string storedFilePath);
    Task<Stream?> GetStreamAsync(string storedFilePath);
    string GetUploadRoot();
}
