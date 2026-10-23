using LearnPath.API.Entities;
using LearnPath.API.Interfaces.Services.Logging;
using LearnPath.API.Services.Audit;

namespace LearnPath.API.Services.Logging;

public class UserActivityLogger : IUserActivityLogger
{
    private readonly ILogger<UserActivityLogger> _logger;
    private readonly string _logDirectory;
    private readonly string _logFilePath;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public UserActivityLogger(ILogger<UserActivityLogger> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        // Prefer ContentRootPath (backend folder) so Logs stays at backend/Logs regardless of working dir
        var basePath = env?.ContentRootPath ?? Directory.GetCurrentDirectory();
        _logDirectory = Path.Combine(basePath, "Logs");
        _logFilePath = Path.Combine(_logDirectory, "info.log");

        try
        {
            Directory.CreateDirectory(_logDirectory);
            if (!File.Exists(_logFilePath))
                File.Create(_logFilePath).Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize user activity log directory/file at {Path}", _logFilePath);
        }
    }

    // For testing or manual construction without IWebHostEnvironment
    public UserActivityLogger(ILogger<UserActivityLogger> logger) : this(logger, null!) { }

    public void Log(string userId, string name, string action)
    {
        var safeUserId = string.IsNullOrWhiteSpace(userId) ? "Unknown" : userId.Trim();
        var safeName = string.IsNullOrWhiteSpace(name) ? "Unknown" : name.Trim();
        var safeAction = string.IsNullOrWhiteSpace(action) ? "Unknown Action" : action.Trim();
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        var consoleMessage = $"[USER ACTIVITY] UserId: {safeUserId} | Name: {safeName} | Action: {safeAction} | Timestamp: {timestamp}";
        var fileMessage = $"UserId: {safeUserId} | Name: {safeName} | Action: {safeAction} | Timestamp: {timestamp}";

        // Console via ILogger — visible in backend console
        try
        {
            _logger.LogInformation("{Message}", consoleMessage);
        }
        catch { /* never break caller */ }

        // File append — thread-safe, append-only
        try
        {
            // Use semaphore to prevent concurrent corruption
            if (!_semaphore.Wait(2000))
            {
                _logger.LogWarning("UserActivityLogger semaphore timeout for action {Action}", safeAction);
                return;
            }

            try
            {
                Directory.CreateDirectory(_logDirectory);
                File.AppendAllText(_logFilePath, fileMessage + Environment.NewLine);
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            // Logging must never break user request
            try { _logger.LogWarning(ex, "Failed to write user activity to info.log for action {Action}", safeAction); } catch { }
        }
    }

    public void Log(AuditAction action, string userId, string name)
    {
        var actionName = MapAuditActionToActivity(action);
        Log(userId, name, actionName);
    }

    private static string MapAuditActionToActivity(AuditAction action) => action switch
    {
        AuditAction.USER_CREATED => "Registered Successfully",
        AuditAction.AUTH_REGISTERED => "Registered Successfully",
        AuditAction.LOGIN => "Login Successful",
        AuditAction.LOGIN_FAILED => "Login Failed",
        AuditAction.LOGOUT => "Logout",
        AuditAction.PASSWORD_CHANGED => "Password Changed",
        AuditAction.PROFILE_UPDATED => "Updated Profile",
        AuditAction.LEARNING_PATH_CREATED => "Created Learning Path",
        AuditAction.LEARNING_PATH_UPDATED => "Updated Learning Path",
        AuditAction.LEARNING_PATH_DELETED => "Deleted Learning Path",
        AuditAction.LEARNING_PATH_PUBLISHED => "Published Learning Path",
        AuditAction.LEARNING_PATH_UNPUBLISHED => "Unpublished Learning Path",
        AuditAction.MODULE_CREATED => "Created Module",
        AuditAction.MODULE_UPDATED => "Updated Module",
        AuditAction.MODULE_DELETED => "Deleted Module",
        AuditAction.MODULE_PUBLISHED => "Published Module",
        AuditAction.MODULE_COMPLETED => "Completed Module",
        AuditAction.CERTIFICATE_GENERATED => "Certificate Generated",
        AuditAction.CLASSROOM_CREATED => "Created Classroom",
        AuditAction.CLASSROOM_JOINED => "Joined Classroom",
        AuditAction.CLASSROOM_LEFT => "Left Classroom",
        AuditAction.ASSIGNMENT_CREATED => "Created Assignment",
        AuditAction.SUBMISSION_UPLOADED => "Submitted Assignment",
        AuditAction.SUBMISSION_GRADED => "Graded Submission",
        AuditAction.QUIZ_CREATED => "Created Quiz",
        AuditAction.QUIZ_STARTED => "Started Quiz",
        AuditAction.QUIZ_COMPLETED => "Submitted Quiz",
        AuditAction.QUIZ_PASSED => "Quiz Passed",
        AuditAction.QUIZ_FAILED => "Quiz Failed",
        AuditAction.POST_CREATED => "Created Post",
        AuditAction.POST_UPDATED => "Updated Post",
        AuditAction.POST_DELETED => "Deleted Post",
        AuditAction.POST_UPVOTED => "Upvoted Post",
        AuditAction.POST_DOWNVOTED => "Downvoted Post",
        AuditAction.COMMENT_CREATED => "Created Comment",
        AuditAction.COMMENT_UPDATED => "Updated Comment",
        AuditAction.COMMENT_DELETED => "Deleted Comment",
        AuditAction.GROUP_CREATED => "Created Group",
        AuditAction.GROUP_JOINED => "Joined Group",
        AuditAction.GROUP_LEFT => "Left Group",
        AuditAction.REPORT_CREATED => "Submitted Report",
        AuditAction.AI_FEEDBACK_GENERATED => "Generated AI Feedback",
        _ => AuditActionCatalog.GetActionName(action)
    };

    public void LogUnknown(string action)
    {
        Log("Unknown", "Unknown", action);
    }
}
