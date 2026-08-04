namespace LearnPath.API.Entities;

public class AuditLog
{
    public long Id { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string? UserId { get; private set; }
    public string? Username { get; private set; }
    public string? Role { get; private set; }
    public AuditAction ActionType { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public string? EntityId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public string? AdditionalData { get; private set; }

    private AuditLog() { }

    public AuditLog(
        AuditAction actionType,
        string entityType,
        string entityId,
        string description,
        string? oldValue = null,
        string? newValue = null,
        string? additionalData = null,
        string? userId = null,
        string? username = null,
        string? role = null)
    {
        Timestamp = DateTime.UtcNow;
        UserId = userId;
        Username = username;
        Role = role;
        ActionType = actionType;
        EntityType = entityType;
        EntityId = entityId;
        Description = description;
        OldValue = oldValue;
        NewValue = newValue;
        AdditionalData = additionalData;
    }
}
