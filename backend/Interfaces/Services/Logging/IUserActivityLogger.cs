namespace LearnPath.API.Interfaces.Services.Logging;

public interface IUserActivityLogger
{
    /// <summary>
    /// Logs a meaningful user activity to console and info.log.
    /// Never logs passwords, tokens, or sensitive data.
    /// Safe to call even if file is unavailable — will not throw.
    /// </summary>
    void Log(string userId, string name, string action);

    /// <summary>
    /// Overload for audit-driven logging using AuditAction.
    /// </summary>
    void Log(Entities.AuditAction action, string userId, string name);

    /// <summary>
    /// Logs activity for anonymous/unknown user (e.g., failed login).
    /// </summary>
    void LogUnknown(string action);
}
