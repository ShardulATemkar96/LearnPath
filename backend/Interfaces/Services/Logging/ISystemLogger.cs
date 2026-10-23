namespace LearnPath.API.Interfaces.Services.Logging;

public interface ISystemLogger
{
    void Log(string message);
    void LogError(string message, Exception? ex = null);
}
