using LearnPath.API.Interfaces.Services.Logging;

namespace LearnPath.API.Services.Logging;

public class SystemLogger : ISystemLogger
{
    private readonly ILogger<SystemLogger> _logger;
    private readonly string _logDirectory;
    private readonly string _logFilePath;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public SystemLogger(ILogger<SystemLogger> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        var basePath = env?.ContentRootPath ?? Directory.GetCurrentDirectory();
        _logDirectory = Path.Combine(basePath, "Logs");
        _logFilePath = Path.Combine(_logDirectory, "system.log");

        try
        {
            Directory.CreateDirectory(_logDirectory);
            if (!File.Exists(_logFilePath))
                File.Create(_logFilePath).Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize system log directory/file at {Path}", _logFilePath);
        }
    }

    public SystemLogger(ILogger<SystemLogger> logger) : this(logger, null!) { }

    public void Log(string message)
    {
        var safeMessage = string.IsNullOrWhiteSpace(message) ? "Unknown system event" : message.Trim();
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var consoleMessage = $"[SYSTEM] {safeMessage} | Timestamp: {timestamp}";
        var fileMessage = $"{safeMessage} | Timestamp: {timestamp}";

        try { _logger.LogInformation("{Message}", consoleMessage); } catch { }

        try
        {
            if (!_semaphore.Wait(2000))
            {
                _logger.LogWarning("SystemLogger semaphore timeout for message {Message}", safeMessage);
                return;
            }
            try
            {
                Directory.CreateDirectory(_logDirectory);
                File.AppendAllText(_logFilePath, fileMessage + Environment.NewLine);
            }
            finally { _semaphore.Release(); }
        }
        catch (Exception ex)
        {
            try { _logger.LogWarning(ex, "Failed to write system log for message {Message}", safeMessage); } catch { }
        }
    }

    public void LogError(string message, Exception? ex = null)
    {
        var safeMessage = string.IsNullOrWhiteSpace(message) ? "System error" : message.Trim();
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var consoleMessage = $"[SYSTEM] {safeMessage} | Timestamp: {timestamp}";
        var fileMessage = $"{safeMessage} | Timestamp: {timestamp}";

        if (ex != null)
            fileMessage += $" | Exception: {ex.GetType().Name}: {ex.Message}";

        try
        {
            if (ex != null) _logger.LogError(ex, "{Message}", consoleMessage);
            else _logger.LogError("{Message}", consoleMessage);
        }
        catch { }

        try
        {
            if (!_semaphore.Wait(2000)) return;
            try
            {
                Directory.CreateDirectory(_logDirectory);
                File.AppendAllText(_logFilePath, fileMessage + Environment.NewLine);
            }
            finally { _semaphore.Release(); }
        }
        catch { }
    }
}
