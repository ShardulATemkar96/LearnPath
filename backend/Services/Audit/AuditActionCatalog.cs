using System.Globalization;
using LearnPath.API.DTOs.Audit;
using LearnPath.API.Entities;

namespace LearnPath.API.Services.Audit;

/// <summary>
/// Presentation metadata for <see cref="AuditAction"/> values.
/// Read-only: it classifies and labels existing actions, it never records them.
/// </summary>
public static class AuditActionCatalog
{
    public static string GetCategory(AuditAction action) => action switch
    {
        AuditAction.LOGIN or
        AuditAction.LOGOUT or
        AuditAction.LOGIN_FAILED or
        AuditAction.USER_CREATED or
        AuditAction.AUTH_REGISTERED => "Authentication",

        AuditAction.PROFILE_UPDATED or
        AuditAction.USERNAME_CHANGED or
        AuditAction.EMAIL_CHANGED or
        AuditAction.PASSWORD_CHANGED or
        AuditAction.USER_ACTIVATED or
        AuditAction.USER_DEACTIVATED or
        AuditAction.USER_SOFT_DELETED or
        AuditAction.USER_MARKED_INVALID or
        AuditAction.INVALID_REMOVED or
        AuditAction.ROLE_CHANGED or
        AuditAction.INSTRUCTOR_ASSIGNED or
        AuditAction.INSTRUCTOR_REMOVED => "Account",

        AuditAction.LEARNING_PATH_CREATED or
        AuditAction.LEARNING_PATH_UPDATED or
        AuditAction.LEARNING_PATH_PUBLISHED or
        AuditAction.LEARNING_PATH_UNPUBLISHED or
        AuditAction.MODULE_CREATED or
        AuditAction.MODULE_UPDATED or
        AuditAction.MODULE_DELETED or
        AuditAction.MODULE_PUBLISHED or
        AuditAction.MODULE_UNPUBLISHED or
        AuditAction.MODULE_COMPLETED or
        AuditAction.CERTIFICATE_GENERATED or
        AuditAction.CERTIFICATE_DELETED or
        AuditAction.CLASSROOM_CREATED or
        AuditAction.CLASSROOM_UPDATED or
        AuditAction.CLASSROOM_JOINED or
        AuditAction.CLASSROOM_LEFT or
        AuditAction.CLASSROOM_DELETED or
        AuditAction.CLASSROOM_MEMBER_REMOVED => "Academic",

        AuditAction.QUIZ_STARTED or
        AuditAction.QUIZ_COMPLETED or
        AuditAction.QUIZ_PASSED or
        AuditAction.QUIZ_FAILED or
        AuditAction.QUIZ_CREATED or
        AuditAction.QUIZ_UPDATED or
        AuditAction.QUIZ_PUBLISHED or
        AuditAction.QUIZ_UNPUBLISHED or
        AuditAction.QUIZ_ARCHIVED or
        AuditAction.QUIZ_DELETED or
        AuditAction.QUIZ_LINKED_TO_MODULE or
        AuditAction.QUIZ_UNLINKED_FROM_MODULE or
        AuditAction.QUESTION_BANK_UPLOADED or
        AuditAction.QUESTION_BANK_VERSION_UPLOADED or
        AuditAction.QUESTION_BANK_ARCHIVED or
        AuditAction.QUESTION_BANK_RESTORED or
        AuditAction.QUESTION_BANK_DELETED => "Assessment",

        _ => "Community",
    };

    /// <summary>Converts SNAKE_CASE enum names into a human readable label.</summary>
    public static string GetActionName(AuditAction action)
    {
        var parts = action.ToString().Split('_', StringSplitOptions.RemoveEmptyEntries);
        var textInfo = CultureInfo.InvariantCulture.TextInfo;

        return string.Join(' ', parts.Select(p => textInfo.ToTitleCase(p.ToLowerInvariant())));
    }

    public static List<AuditActionOptionDto> GetOptions() =>
        Enum.GetValues<AuditAction>()
            .Select(a => new AuditActionOptionDto
            {
                ActionType = a,
                ActionName = GetActionName(a),
                Category   = GetCategory(a),
            })
            .OrderBy(o => o.Category)
            .ThenBy(o => o.ActionName)
            .ToList();
}
