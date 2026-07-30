using LearnPath.API.Configuration;
using LearnPath.API.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace LearnPath.API.Services.Submission;

public class FileStorageService : IFileStorageService
{
    private readonly string _uploadRoot;

    public FileStorageService(IOptions<UploadSettings> settings)
    {
        _uploadRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", settings.Value.UploadDirectory));
        Directory.CreateDirectory(_uploadRoot);
    }

    public string GetUploadRoot() => _uploadRoot;

    public async Task<FileStorageResult> StoreAsync(int assignmentId, string userId, Stream fileStream, string extension)
    {
        var relativePath = Path.Combine("assignments", $"assignment-{assignmentId}", $"student-{userId}");
        var fullDir = Path.GetFullPath(Path.Combine(_uploadRoot, relativePath));

        if (!fullDir.StartsWith(_uploadRoot, StringComparison.OrdinalIgnoreCase))
        {
            return new FileStorageResult
            {
                Success = false,
                ErrorMessage = "Invalid storage path."
            };
        }

        Directory.CreateDirectory(fullDir);

        var storedFileName = $"submission.{extension}";
        var fullPath = Path.Combine(fullDir, storedFileName);

        var tempPath = fullPath + ".tmp";
        try
        {
            using (var tempStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
            {
                await fileStream.CopyToAsync(tempStream);
            }

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            File.Move(tempPath, fullPath);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
            throw;
        }

        var storedPath = Path.Combine(relativePath, storedFileName).Replace("\\", "/");
        return new FileStorageResult
        {
            Success = true,
            StoredFilePath = storedPath
        };
    }

    public Task DeleteAsync(string storedFilePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_uploadRoot, storedFilePath));

        if (fullPath.StartsWith(_uploadRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public Task<Stream?> GetStreamAsync(string storedFilePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_uploadRoot, storedFilePath));

        if (!fullPath.StartsWith(_uploadRoot, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult<Stream?>(null);

        if (!File.Exists(fullPath))
            return Task.FromResult<Stream?>(null);

        return Task.FromResult<Stream?>(new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read));
    }
}
