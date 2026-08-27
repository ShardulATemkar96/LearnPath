using LearnPath.API.Data;
using LearnPath.API.DTOs.Ai;
using LearnPath.API.DTOs.Classroom;
using LearnPath.API.Entities;
using LearnPath.API.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace LearnPath.API.Services.Classroom;

public class ClassroomService : IClassroomService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileValidationService _fileValidator;
    private readonly IFileStorageService _fileStorage;
    private readonly IAuditLogService _auditLog;

    public ClassroomService(
        ApplicationDbContext context,
        IFileValidationService fileValidator,
        IFileStorageService fileStorage,
        IAuditLogService auditLog)
    {
        _context = context;
        _fileValidator = fileValidator;
        _fileStorage = fileStorage;
        _auditLog = auditLog;
    }

    // ── Classrooms ────────────────────────────────────────────

    public async Task<List<ClassroomResponseDto>> GetMyClassroomsAsync(string userId)
    {
        return await _context.UserClassrooms
            .Where(uc => uc.UserId == userId)
            .Include(uc => uc.Classroom)
                .ThenInclude(c => c.CreatedBy)
            .Include(uc => uc.Classroom)
                .ThenInclude(c => c.LearningPath)
            .Include(uc => uc.Classroom)
                .ThenInclude(c => c.UserClassrooms)
            .Select(uc => MapToResponse(uc.Classroom, uc.Role, userId))
            .ToListAsync();
    }

    public async Task<ClassroomDetailResponseDto> GetByIdAsync(int id, string userId)
    {
        var membership = await _context.UserClassrooms
            .FirstOrDefaultAsync(uc => uc.ClassroomId == id && uc.UserId == userId)
            ?? throw new UnauthorizedAccessException("You are not a member of this classroom.");

        var classroom = await _context.Classrooms
            .Include(c => c.CreatedBy)
            .Include(c => c.LearningPath)
            .Include(c => c.UserClassrooms)
                .ThenInclude(uc => uc.User)
            .Include(c => c.Assignments)
                .ThenInclude(a => a.Submissions)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("Classroom not found.");

        var members = classroom.UserClassrooms.Select(uc => new ClassroomMemberDto
        {
            UserId = uc.UserId,
            FullName = $"{uc.User.FirstName} {uc.User.LastName}",
            Email = uc.User.Email!,
            Role = uc.Role,
            Status = uc.User.Status,
            InvalidReason = uc.User.InvalidReason,
            JoinedAt = uc.JoinedAt,
        }).ToList();

        var isInstructor = membership.Role == "Instructor";
        var sortedAssignments = classroom.Assignments.OrderBy(a => a.Id).ToList();
        var assignments = sortedAssignments.Select((a, idx) =>
        {
            var mySubmission = a.Submissions.FirstOrDefault(s => s.UserId == userId);
            var isCompleted = mySubmission?.Grade >= 5;
            bool isUnlocked;
            if (isInstructor)
            {
                isUnlocked = true;
            }
            else if (idx == 0)
            {
                isUnlocked = true;
            }
            else
            {
                var prevAssignment = sortedAssignments[idx - 1];
                var prevSubmission = prevAssignment.Submissions.FirstOrDefault(s => s.UserId == userId);
                isUnlocked = prevSubmission?.Grade >= 5;
            }

            // For locked assignments, don't expose description to students
            var description = a.Description;
            if (!isUnlocked && !isInstructor)
            {
                description = string.Empty;
            }

            return new AssignmentResponseDto
            {
                Id = a.Id,
                Title = a.Title,
                Description = description,
                DueDate = a.DueDate,
                ClassroomId = a.ClassroomId,
                SubmissionCount = a.Submissions.Count,
                HasSubmitted = mySubmission != null,
                MySubmissionId = mySubmission?.Id,
                MyOriginalFileName = mySubmission?.OriginalFileName,
                MySubmissionStatus = mySubmission?.Status,
                MyGrade = mySubmission?.Grade,
                MyFeedback = mySubmission?.Feedback,
                CreatedAt = a.CreatedAt,
                IsUnlocked = isUnlocked,
                IsCompleted = isCompleted,
            };
        }).ToList();

        return new ClassroomDetailResponseDto
        {
            Id = classroom.Id,
            Title = classroom.Title,
            Description = classroom.Description,
            InviteCode = classroom.InviteCode,
            LearningPathId = classroom.LearningPathId,
            LearningPathTitle = classroom.LearningPath.Title,
            CreatedById = classroom.CreatedById,
            CreatedByName = $"{classroom.CreatedBy.FirstName} {classroom.CreatedBy.LastName}",
            MemberCount = classroom.UserClassrooms.Count,
            UserRole = membership.Role,
            CreatedAt = classroom.CreatedAt,
            Members = members,
            Assignments = assignments,
        };
    }

    public async Task<ClassroomResponseDto> CreateAsync(CreateClassroomDto dto, string userId)
    {
        var pathExists = await _context.LearningPaths
            .AnyAsync(p => p.Id == dto.LearningPathId);
        if (!pathExists) throw new KeyNotFoundException("Learning path not found.");

        var classroom = new Entities.Classroom
        {
            Title = dto.Title,
            Description = dto.Description,
            LearningPathId = dto.LearningPathId,
            CreatedById = userId,
            InviteCode = GenerateInviteCode(),
        };

        await _context.Classrooms.AddAsync(classroom);
        await _context.SaveChangesAsync();

        // Auto-join as Instructor
        await _context.UserClassrooms.AddAsync(new UserClassroom
        {
            UserId = userId,
            ClassroomId = classroom.Id,
            Role = "Instructor",
        });
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.CLASSROOM_CREATED,
            "Classroom",
            classroom.Id.ToString(),
            $"Classroom '{classroom.Title}' was created.",
            additionalData: $"LearningPathId: {classroom.LearningPathId}",
            userId: userId);

        await _context.Entry(classroom).Reference(c => c.CreatedBy).LoadAsync();
        await _context.Entry(classroom).Reference(c => c.LearningPath).LoadAsync();
        await _context.Entry(classroom).Collection(c => c.UserClassrooms).LoadAsync();

        return MapToResponse(classroom, "Instructor", userId);
    }

    public async Task<ClassroomResponseDto> UpdateAsync(
        int id, UpdateClassroomDto dto, string userId)
    {
        var classroom = await GetOwnedClassroomAsync(id, userId);

        var oldValue = $"Title: {classroom.Title}; Description: {classroom.Description}";

        classroom.Title = dto.Title;
        classroom.Description = dto.Description;
        classroom.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.CLASSROOM_UPDATED,
            "Classroom",
            classroom.Id.ToString(),
            $"Classroom '{classroom.Title}' was updated.",
            oldValue: oldValue,
            newValue: $"Title: {classroom.Title}; Description: {classroom.Description}",
            userId: userId);

        return MapToResponse(classroom, "Instructor", userId);
    }

    public async Task DeleteAsync(int id, string userId)
    {
        var classroom = await GetOwnedClassroomAsync(id, userId);
        _context.Classrooms.Remove(classroom);
        await _context.SaveChangesAsync();
    }

    public async Task JoinAsync(string inviteCode, string userId)
    {
        var classroom = await _context.Classrooms
            .FirstOrDefaultAsync(c => c.InviteCode == inviteCode)
            ?? throw new KeyNotFoundException("Invalid invite code.");

        var already = await _context.UserClassrooms
            .AnyAsync(uc => uc.ClassroomId == classroom.Id && uc.UserId == userId);
        if (already) throw new ArgumentException("You are already a member.");

        await _context.UserClassrooms.AddAsync(new UserClassroom
        {
            UserId = userId,
            ClassroomId = classroom.Id,
            Role = "Student",
        });
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.CLASSROOM_JOINED,
            "Classroom",
            classroom.Id.ToString(),
            $"Joined classroom '{classroom.Title}'.",
            userId: userId);
    }

    public async Task LeaveAsync(int id, string userId)
    {
        var membership = await _context.UserClassrooms
            .FirstOrDefaultAsync(uc => uc.ClassroomId == id && uc.UserId == userId)
            ?? throw new KeyNotFoundException("You are not a member.");

        if (membership.Role == "Instructor")
            throw new ArgumentException("Instructors cannot leave. Transfer ownership first.");

        _context.UserClassrooms.Remove(membership);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.CLASSROOM_LEFT,
            "Classroom",
            id.ToString(),
            "Left classroom.",
            userId: userId);
    }

    public async Task RemoveMemberAsync(int classroomId, string memberUserId, string userId)
    {
        await EnsureInstructorAsync(classroomId, userId);

        var membership = await _context.UserClassrooms
            .FirstOrDefaultAsync(uc => uc.ClassroomId == classroomId && uc.UserId == memberUserId)
            ?? throw new KeyNotFoundException("Member not found.");

        if (membership.Role == "Instructor")
            throw new ArgumentException("Cannot remove an instructor.");

        _context.UserClassrooms.Remove(membership);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.CLASSROOM_MEMBER_REMOVED,
            "Classroom",
            classroomId.ToString(),
            "A member was removed from the classroom.",
            additionalData: $"RemovedUserId: {memberUserId}",
            userId: userId);
    }

    public async Task<List<ClassroomMemberDto>> GetMembersAsync(int id, string userId)
    {
        var isMember = await _context.UserClassrooms
            .AnyAsync(uc => uc.ClassroomId == id && uc.UserId == userId);
        if (!isMember) throw new UnauthorizedAccessException("Access denied.");

        return await _context.UserClassrooms
            .Where(uc => uc.ClassroomId == id)
            .Include(uc => uc.User)
            .Select(uc => new ClassroomMemberDto
            {
                UserId = uc.UserId,
                FullName = $"{uc.User.FirstName} {uc.User.LastName}",
                Email = uc.User.Email!,
                Role = uc.Role,
                Status = uc.User.Status,
                InvalidReason = uc.User.InvalidReason,
                JoinedAt = uc.JoinedAt,
            })
            .ToListAsync();
    }

    public async Task<ClassroomMemberDto> MarkInvalidAsync(
        int classroomId, string memberUserId, MarkInvalidDto dto, string userId)
    {
        await EnsureInstructorAsync(classroomId, userId);

        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new ArgumentException("A reason is required.");

        var membership = await _context.UserClassrooms
            .Where(uc => uc.ClassroomId == classroomId && uc.UserId == memberUserId)
            .Include(uc => uc.User)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Member not found.");

        if (membership.Role != "Student")
            throw new ArgumentException("Only students can be marked invalid.");

        if (membership.User.IsSuperAdmin)
            throw new ArgumentException("The Super Admin account cannot be marked invalid.");

        membership.User.Status = UserStatus.Invalid;
        membership.User.InvalidReason = dto.Reason.Trim();
        membership.User.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.USER_MARKED_INVALID,
            "User",
            membership.UserId,
            $"User '{membership.User.Email}' was marked invalid by a classroom instructor.",
            oldValue: "Active",
            newValue: "Invalid",
            additionalData: $"ClassroomId: {classroomId}; Reason: {dto.Reason.Trim()}",
            userId: userId);

        return new ClassroomMemberDto
        {
            UserId = membership.UserId,
            FullName = $"{membership.User.FirstName} {membership.User.LastName}",
            Email = membership.User.Email!,
            Role = membership.Role,
            Status = membership.User.Status,
            InvalidReason = membership.User.InvalidReason,
            JoinedAt = membership.JoinedAt,
        };
    }

    // ── Assignments ───────────────────────────────────────────

    public async Task<AssignmentResponseDto> CreateAssignmentAsync(
        int classroomId, CreateAssignmentDto dto, string userId)
    {
        await EnsureAdminAsync(classroomId, userId);

        var assignment = new Assignment
        {
            Title = dto.Title,
            Description = dto.Description,
            DueDate = dto.DueDate,
            ClassroomId = classroomId,
        };

        await _context.Assignments.AddAsync(assignment);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.ASSIGNMENT_CREATED,
            "Assignment",
            assignment.Id.ToString(),
            $"Assignment '{assignment.Title}' was created.",
            additionalData: $"ClassroomId: {classroomId}",
            userId: userId);

        return new AssignmentResponseDto
        {
            Id = assignment.Id,
            Title = assignment.Title,
            Description = assignment.Description,
            DueDate = assignment.DueDate,
            ClassroomId = assignment.ClassroomId,
            SubmissionCount = 0,
            HasSubmitted = false,
            CreatedAt = assignment.CreatedAt,
        };
    }

    public async Task<AssignmentResponseDto> UpdateAssignmentAsync(
        int classroomId, int assignmentId, UpdateAssignmentDto dto, string userId)
    {
        await EnsureInstructorAsync(classroomId, userId);
        var assignment = await GetAssignmentAsync(assignmentId, classroomId);

        assignment.Title = dto.Title;
        assignment.Description = dto.Description;
        assignment.DueDate = dto.DueDate;
        assignment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.ASSIGNMENT_UPDATED,
            "Assignment",
            assignment.Id.ToString(),
            $"Assignment '{assignment.Title}' was updated.",
            additionalData: $"ClassroomId: {classroomId}",
            userId: userId);

        return new AssignmentResponseDto
        {
            Id = assignment.Id,
            Title = assignment.Title,
            Description = assignment.Description,
            DueDate = assignment.DueDate,
            ClassroomId = assignment.ClassroomId,
            CreatedAt = assignment.CreatedAt,
        };
    }

    public async Task DeleteAssignmentAsync(int classroomId, int assignmentId, string userId)
    {
        await EnsureInstructorAsync(classroomId, userId);
        var assignment = await GetAssignmentAsync(assignmentId, classroomId);
        _context.Assignments.Remove(assignment);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.ASSIGNMENT_DELETED,
            "Assignment",
            assignmentId.ToString(),
            $"Assignment '{assignment.Title}' was deleted.",
            additionalData: $"ClassroomId: {classroomId}",
            userId: userId);
    }

    // ── Submissions ───────────────────────────────────────────

    public async Task<SubmissionResponseDto> UploadSubmissionAsync(
        int classroomId, int assignmentId, IFormFile file, string userId)
    {
        var membership = await _context.UserClassrooms
            .FirstOrDefaultAsync(uc => uc.ClassroomId == classroomId && uc.UserId == userId)
            ?? throw new UnauthorizedAccessException("Not a member of this classroom.");

        var assignment = await _context.Assignments
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.ClassroomId == classroomId)
            ?? throw new KeyNotFoundException("Assignment not found.");

        await EnsureAssignmentUnlockedAsync(classroomId, assignmentId, userId);

        var validation = _fileValidator.Validate(file);
        if (!validation.IsValid)
            throw new ArgumentException(validation.ErrorMessage);

        var existing = await _context.Submissions
            .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.UserId == userId);

        if (existing != null)
        {
            if (existing.Status is SubmissionStatus.UnderReview or SubmissionStatus.Reviewed or SubmissionStatus.Graded)
                throw new InvalidOperationException("Cannot replace submission in its current status.");
        }

        var now = DateTime.UtcNow;
        var isLate = assignment.DueDate < now;

        using var stream = file.OpenReadStream();
        var storage = await _fileStorage.StoreAsync(assignmentId, userId, stream, validation.Extension!);
        if (!storage.Success)
            throw new InvalidOperationException(storage.ErrorMessage ?? "File storage failed.");

        string newStatus;
        if (existing != null)
        {
            if (existing.StoredFilePath is not null && existing.StoredFilePath != storage.StoredFilePath)
                await _fileStorage.DeleteAsync(existing.StoredFilePath);

            newStatus = existing.Status == SubmissionStatus.ReturnedForResubmission
                ? SubmissionStatus.SubmittedAgain
                : SubmissionStatus.Submitted;

            existing.OriginalFileName = file.FileName;
            existing.StoredFilePath = storage.StoredFilePath;
            existing.FileExtension = validation.Extension;
            existing.FileSize = file.Length;
            existing.MimeType = validation.MimeType;
            existing.IsLate = isLate;
            existing.Status = newStatus;
            existing.SubmittedAt = now;
            existing.UpdatedAt = now;
        }
        else
        {
            newStatus = SubmissionStatus.Submitted;
            existing = new Entities.Submission
            {
                AssignmentId = assignmentId,
                UserId = userId,
                OriginalFileName = file.FileName,
                StoredFilePath = storage.StoredFilePath,
                FileExtension = validation.Extension,
                FileSize = file.Length,
                MimeType = validation.MimeType,
                IsLate = isLate,
                Status = newStatus,
                SubmittedAt = now,
                UpdatedAt = now,
            };
            _context.Submissions.Add(existing);
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch
        {
            if (storage.StoredFilePath is not null)
                await _fileStorage.DeleteAsync(storage.StoredFilePath);
            throw;
        }

        await _auditLog.LogAsync(
            AuditAction.SUBMISSION_UPLOADED,
            "Submission",
            existing.Id.ToString(),
            $"Submission uploaded for assignment {assignmentId}.",
            newValue: $"Status: {existing.Status}",
            additionalData: $"ClassroomId: {classroomId}",
            userId: userId);

        var user = await _context.Users.FindAsync(userId);

        return new SubmissionResponseDto
        {
            Id = existing.Id,
            AssignmentId = existing.AssignmentId,
            UserId = existing.UserId,
            UserFullName = $"{user!.FirstName} {user.LastName}",
            OriginalFileName = existing.OriginalFileName,
            FileExtension = existing.FileExtension,
            FileSize = existing.FileSize,
            MimeType = existing.MimeType,
            IsLate = existing.IsLate,
            Status = existing.Status,
            SubmittedAt = existing.SubmittedAt,
        };
    }

    public async Task<SubmissionResponseDto> GetMySubmissionAsync(
        int classroomId, int assignmentId, string userId)
    {
        var isMember = await _context.UserClassrooms
            .AnyAsync(uc => uc.ClassroomId == classroomId && uc.UserId == userId);
        if (!isMember)
            throw new UnauthorizedAccessException("Not a member.");

        await EnsureAssignmentUnlockedAsync(classroomId, assignmentId, userId);

        var submission = await _context.Submissions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.UserId == userId);

        if (submission is null)
        {
            var user = await _context.Users.FindAsync(userId);
            return new SubmissionResponseDto
            {
                AssignmentId = assignmentId,
                UserId = userId,
                UserFullName = $"{user!.FirstName} {user.LastName}",
                Status = SubmissionStatus.NotSubmitted,
            };
        }

        var dto = new SubmissionResponseDto
        {
            Id = submission.Id,
            AssignmentId = submission.AssignmentId,
            UserId = submission.UserId,
            UserFullName = $"{submission.User.FirstName} {submission.User.LastName}",
            OriginalFileName = submission.OriginalFileName,
            FileExtension = submission.FileExtension,
            FileSize = submission.FileSize,
            MimeType = submission.MimeType,
            IsLate = submission.IsLate,
            Status = submission.Status,
            Feedback = submission.Feedback,
            Grade = submission.Grade,
            SubmittedAt = submission.SubmittedAt,
            PublishedAt = submission.PublishedAt,
        };

        if (submission.PublishedAt is not null)
        {
            var aiFeedback = await _context.SubmissionAiFeedbacks
                .FirstOrDefaultAsync(f => f.SubmissionId == submission.Id);

            if (aiFeedback is not null)
            {
                dto.AiFeedback = new AiFeedbackResponseDto
                {
                    Summary = aiFeedback.Summary,
                    GrammarFeedback = aiFeedback.GrammarFeedback,
                    RubricCoverage = aiFeedback.RubricCoverage,
                    MissingTopics = aiFeedback.MissingTopics,
                    SuggestedScore = new SuggestedScoreDto
                    {
                        Percentage = aiFeedback.SuggestedScorePercentage,
                        Marks = aiFeedback.SuggestedScoreMarks,
                    },
                    OverallRecommendation = aiFeedback.OverallRecommendation,
                    Disclaimer = aiFeedback.Disclaimer,
                    GeneratedAt = aiFeedback.GeneratedAt,
                };
            }
        }

        return dto;
    }

    public async Task DeleteSubmissionAsync(int classroomId, int assignmentId, string userId)
    {
        var isMember = await _context.UserClassrooms
            .AnyAsync(uc => uc.ClassroomId == classroomId && uc.UserId == userId);
        if (!isMember) throw new UnauthorizedAccessException("Not a member.");

        var submission = await _context.Submissions
            .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.UserId == userId)
            ?? throw new KeyNotFoundException("Submission not found.");

        if (submission.Status != SubmissionStatus.Submitted && submission.Status != SubmissionStatus.SubmittedAgain)
            throw new ArgumentException("Can only cancel a submitted submission.");

        _context.Submissions.Remove(submission);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.SUBMISSION_DELETED,
            "Submission",
            submission.Id.ToString(),
            $"Submission deleted for assignment {assignmentId}.",
            additionalData: $"ClassroomId: {classroomId}",
            userId: userId);
    }

    public async Task<List<SubmissionResponseDto>> GetSubmissionsAsync(
        int classroomId, int assignmentId, string userId)
    {
        await EnsureInstructorAsync(classroomId, userId);

        var assignment = await _context.Assignments
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.ClassroomId == classroomId)
            ?? throw new KeyNotFoundException("Assignment not found.");

        var submissions = await _context.Submissions
            .Where(s => s.AssignmentId == assignmentId)
            .Include(s => s.User)
            .ToListAsync();

        var students = await _context.UserClassrooms
            .Where(uc => uc.ClassroomId == classroomId && uc.Role == "Student")
            .Include(uc => uc.User)
            .ToListAsync();

        return students.Select(s =>
        {
            var sub = submissions.FirstOrDefault(x => x.UserId == s.UserId);
            return new SubmissionResponseDto
            {
                Id = sub?.Id ?? 0,
                AssignmentId = assignmentId,
                UserId = s.UserId,
                UserFullName = $"{s.User.FirstName} {s.User.LastName}",
                OriginalFileName = sub?.OriginalFileName,
                FileExtension = sub?.FileExtension,
                FileSize = sub?.FileSize,
                MimeType = sub?.MimeType,
                IsLate = sub?.IsLate ?? false,
                Status = sub?.Status ?? SubmissionStatus.NotSubmitted,
                Feedback = sub?.Feedback,
                Grade = sub?.Grade,
                SubmittedAt = sub?.SubmittedAt ?? default,
                PublishedAt = sub?.PublishedAt,
            };
        })
            .OrderByDescending(s => s.SubmittedAt)
            .ToList();
    }

    public async Task<SubmissionResponseDto> GetSubmissionByIdAsync(int submissionId, string userId)
    {
        var submission = await _context.Submissions
            .Include(s => s.User)
            .Include(s => s.Assignment)
            .FirstOrDefaultAsync(s => s.Id == submissionId)
            ?? throw new KeyNotFoundException("Submission not found.");

        var classroomId = submission.Assignment.ClassroomId;
        var isInstructor = await _context.UserClassrooms
            .AnyAsync(uc => uc.ClassroomId == classroomId && uc.UserId == userId && uc.Role == "Instructor");

        if (!isInstructor)
            throw new UnauthorizedAccessException("Access denied.");

        return new SubmissionResponseDto
        {
            Id = submission.Id,
            AssignmentId = submission.AssignmentId,
            UserId = submission.UserId,
            UserFullName = $"{submission.User.FirstName} {submission.User.LastName}",
            OriginalFileName = submission.OriginalFileName,
            FileExtension = submission.FileExtension,
            FileSize = submission.FileSize,
            MimeType = submission.MimeType,
            IsLate = submission.IsLate,
            Status = submission.Status,
            Feedback = submission.Feedback,
            Grade = submission.Grade,
            SubmittedAt = submission.SubmittedAt,
        };
    }

    public async Task<(Stream? FileStream, string? FileName, string? MimeType)> GetPreviewFileAsync(
        int submissionId, string userId)
    {
        var (stream, fileName, mimeType) = await GetAuthorizedFileStreamAsync(submissionId, userId);
        return (stream, fileName, mimeType);
    }

    public async Task<(Stream? FileStream, string? FileName, string? MimeType)> GetDownloadFileAsync(
        int submissionId, string userId)
    {
        return await GetAuthorizedFileStreamAsync(submissionId, userId);
    }

    private async Task<(Stream? FileStream, string? FileName, string? MimeType)> GetAuthorizedFileStreamAsync(
        int submissionId, string userId)
    {
        var submission = await _context.Submissions
            .Include(s => s.Assignment)
                .ThenInclude(a => a.Classroom)
            .FirstOrDefaultAsync(s => s.Id == submissionId)
            ?? throw new KeyNotFoundException("Submission not found.");

        var isOwner = submission.UserId == userId;
        var isInstructor = await _context.UserClassrooms
            .AnyAsync(uc => uc.ClassroomId == submission.Assignment.ClassroomId && uc.UserId == userId && uc.Role == "Instructor");

        if (!isOwner && !isInstructor)
            throw new UnauthorizedAccessException("Access denied.");

        if (submission.StoredFilePath is null)
            throw new KeyNotFoundException("No file uploaded.");

        var stream = await _fileStorage.GetStreamAsync(submission.StoredFilePath);
        if (stream is null)
            throw new KeyNotFoundException("File not found on disk.");

        return (stream, submission.OriginalFileName, submission.MimeType);
    }

    public async Task<SubmissionResponseDto> TransitionStatusAsync(
        int submissionId, TransitionStatusDto dto, string userId)
    {
        var submission = await _context.Submissions
            .Include(s => s.User)
            .Include(s => s.Assignment)
            .FirstOrDefaultAsync(s => s.Id == submissionId)
            ?? throw new KeyNotFoundException("Submission not found.");

        var classroomId = submission.Assignment.ClassroomId;
        await EnsureInstructorAsync(classroomId, userId);

        var targetStatus = dto.Status;
        if (targetStatus != SubmissionStatus.UnderReview && targetStatus != SubmissionStatus.Reviewed)
            throw new ArgumentException("Invalid status transition. Only UNDER_REVIEW and REVIEWED are permitted.");

        if (targetStatus == SubmissionStatus.UnderReview &&
            submission.Status != SubmissionStatus.Submitted &&
            submission.Status != SubmissionStatus.SubmittedAgain)
            throw new InvalidOperationException($"Cannot transition from {submission.Status} to UNDER_REVIEW.");

        if (targetStatus == SubmissionStatus.Reviewed &&
            submission.Status != SubmissionStatus.UnderReview)
            throw new InvalidOperationException($"Cannot transition from {submission.Status} to REVIEWED.");

        var oldStatus = submission.Status;
        submission.Status = targetStatus;
        submission.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.SUBMISSION_STATUS_CHANGED,
            "Submission",
            submission.Id.ToString(),
            $"Submission {submission.Id} status changed from {oldStatus} to {submission.Status}.",
            oldValue: oldStatus.ToString(),
            newValue: submission.Status.ToString(),
            additionalData: $"ClassroomId: {classroomId}",
            userId: userId);

        return new SubmissionResponseDto
        {
            Id = submission.Id,
            AssignmentId = submission.AssignmentId,
            UserId = submission.UserId,
            UserFullName = $"{submission.User.FirstName} {submission.User.LastName}",
            OriginalFileName = submission.OriginalFileName,
            FileExtension = submission.FileExtension,
            FileSize = submission.FileSize,
            MimeType = submission.MimeType,
            IsLate = submission.IsLate,
            Status = submission.Status,
            Feedback = submission.Feedback,
            Grade = submission.Grade,
            SubmittedAt = submission.SubmittedAt,
        };
    }

    public async Task<SubmissionResponseDto> ReturnForResubmissionAsync(
        int submissionId, ReturnSubmissionDto dto, string userId)
    {
        var submission = await _context.Submissions
            .Include(s => s.User)
            .Include(s => s.Assignment)
            .FirstOrDefaultAsync(s => s.Id == submissionId)
            ?? throw new KeyNotFoundException("Submission not found.");

        var classroomId = submission.Assignment.ClassroomId;
        await EnsureInstructorAsync(classroomId, userId);

        if (submission.Status is SubmissionStatus.Graded)
            throw new InvalidOperationException("Cannot return a graded submission for resubmission.");

        if (submission.StoredFilePath is null)
            throw new InvalidOperationException("Cannot return a submission with no uploaded file.");

        submission.Status = SubmissionStatus.ReturnedForResubmission;
        submission.Feedback = dto.Feedback;
        submission.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.SUBMISSION_RETURNED,
            "Submission",
            submission.Id.ToString(),
            $"Submission {submission.Id} returned for resubmission.",
            newValue: SubmissionStatus.ReturnedForResubmission.ToString(),
            additionalData: $"ClassroomId: {classroomId}",
            userId: userId);

        return new SubmissionResponseDto
        {
            Id = submission.Id,
            AssignmentId = submission.AssignmentId,
            UserId = submission.UserId,
            UserFullName = $"{submission.User.FirstName} {submission.User.LastName}",
            OriginalFileName = submission.OriginalFileName,
            FileExtension = submission.FileExtension,
            FileSize = submission.FileSize,
            MimeType = submission.MimeType,
            IsLate = submission.IsLate,
            Status = submission.Status,
            Feedback = submission.Feedback,
            Grade = submission.Grade,
            SubmittedAt = submission.SubmittedAt,
        };
    }

    public async Task<SubmissionResponseDto> GradeSubmissionAsync(
        int classroomId, int assignmentId, int submissionId,
        GradeSubmissionDto dto, string userId)
    {
        await EnsureInstructorAsync(classroomId, userId);

        var submission = await _context.Submissions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == submissionId && s.AssignmentId == assignmentId)
            ?? throw new KeyNotFoundException("Submission not found.");

        submission.Grade = dto.Grade;
        submission.Feedback = dto.Feedback;
        submission.Status = SubmissionStatus.Graded;
        submission.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.SUBMISSION_GRADED,
            "Submission",
            submission.Id.ToString(),
            $"Submission {submission.Id} graded.",
            newValue: $"Grade: {submission.Grade}",
            additionalData: $"ClassroomId: {classroomId}",
            userId: userId);

        return new SubmissionResponseDto
        {
            Id = submission.Id, AssignmentId = submission.AssignmentId,
            UserId = submission.UserId,
            UserFullName = $"{submission.User.FirstName} {submission.User.LastName}",
            OriginalFileName = submission.OriginalFileName,
            FileExtension = submission.FileExtension,
            FileSize = submission.FileSize,
            MimeType = submission.MimeType,
            IsLate = submission.IsLate,
            Status = submission.Status,
            Feedback = submission.Feedback, Grade = submission.Grade,
            SubmittedAt = submission.SubmittedAt,
        };
    }

    public async Task<SubmissionResponseDto> VerifySubmissionAsync(
        int classroomId, int assignmentId, int submissionId, string userId)
    {
        await EnsureInstructorAsync(classroomId, userId);

        var submission = await _context.Submissions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == submissionId && s.AssignmentId == assignmentId)
            ?? throw new KeyNotFoundException("Submission not found.");

        submission.Status = SubmissionStatus.UnderReview;
        submission.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.SUBMISSION_VERIFIED,
            "Submission",
            submission.Id.ToString(),
            $"Submission {submission.Id} verified (moved to {submission.Status}).",
            newValue: submission.Status.ToString(),
            additionalData: $"ClassroomId: {classroomId}",
            userId: userId);

        return new SubmissionResponseDto
        {
            Id = submission.Id, AssignmentId = submission.AssignmentId,
            UserId = submission.UserId,
            UserFullName = $"{submission.User.FirstName} {submission.User.LastName}",
            OriginalFileName = submission.OriginalFileName,
            FileExtension = submission.FileExtension,
            FileSize = submission.FileSize,
            MimeType = submission.MimeType,
            IsLate = submission.IsLate,
            Status = submission.Status,
            Feedback = submission.Feedback, Grade = submission.Grade,
            SubmittedAt = submission.SubmittedAt,
        };
    }

    public async Task<SubmissionResponseDto> CompleteSubmissionAsync(
        int classroomId, int assignmentId, int submissionId, string userId)
    {
        await EnsureInstructorAsync(classroomId, userId);

        var submission = await _context.Submissions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == submissionId && s.AssignmentId == assignmentId)
            ?? throw new KeyNotFoundException("Submission not found.");

        if (submission.Grade == null)
            throw new ArgumentException("Grade must be set before completing.");

        var oldStatus = submission.Status;
        submission.Status = submission.Grade >= 5 ? SubmissionStatus.Graded : SubmissionStatus.ReturnedForResubmission;
        submission.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.SUBMISSION_COMPLETED,
            "Submission",
            submission.Id.ToString(),
            $"Submission {submission.Id} completed.",
            oldValue: oldStatus.ToString(),
            newValue: submission.Status.ToString(),
            additionalData: $"ClassroomId: {classroomId}; Grade: {submission.Grade}",
            userId: userId);

        return new SubmissionResponseDto
        {
            Id = submission.Id, AssignmentId = submission.AssignmentId,
            UserId = submission.UserId,
            UserFullName = $"{submission.User.FirstName} {submission.User.LastName}",
            OriginalFileName = submission.OriginalFileName,
            FileExtension = submission.FileExtension,
            FileSize = submission.FileSize,
            MimeType = submission.MimeType,
            IsLate = submission.IsLate,
            Status = submission.Status,
            Feedback = submission.Feedback, Grade = submission.Grade,
            SubmittedAt = submission.SubmittedAt,
        };
    }

    public async Task<SubmissionResponseDto> PublishEvaluationAsync(
        int classroomId, int assignmentId, int submissionId, string userId)
    {
        await EnsureInstructorAsync(classroomId, userId);

        var submission = await _context.Submissions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == submissionId && s.AssignmentId == assignmentId)
            ?? throw new KeyNotFoundException("Submission not found.");

        if (submission.Status != SubmissionStatus.Graded)
            throw new InvalidOperationException("Submission must be graded before publishing.");

        submission.PublishedAt = DateTime.UtcNow;
        submission.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync(
            AuditAction.SUBMISSION_PUBLISHED,
            "Submission",
            submission.Id.ToString(),
            $"Evaluation for submission {submission.Id} published.",
            newValue: submission.PublishedAt.ToString(),
            additionalData: $"ClassroomId: {classroomId}",
            userId: userId);

        return new SubmissionResponseDto
        {
            Id = submission.Id, AssignmentId = submission.AssignmentId,
            UserId = submission.UserId,
            UserFullName = $"{submission.User.FirstName} {submission.User.LastName}",
            OriginalFileName = submission.OriginalFileName,
            FileExtension = submission.FileExtension,
            FileSize = submission.FileSize,
            MimeType = submission.MimeType,
            IsLate = submission.IsLate,
            Status = submission.Status,
            Feedback = submission.Feedback, Grade = submission.Grade,
            SubmittedAt = submission.SubmittedAt,
            PublishedAt = submission.PublishedAt,
        };
    }

    private async Task EnsureAssignmentUnlockedAsync(int classroomId, int assignmentId, string userId)
    {
        var membership = await _context.UserClassrooms
            .FirstOrDefaultAsync(uc => uc.ClassroomId == classroomId && uc.UserId == userId);
        if (membership == null) return;
        if (membership.Role == "Instructor") return;

        var assignments = await _context.Assignments
            .Where(a => a.ClassroomId == classroomId)
            .OrderBy(a => a.Id)
            .Include(a => a.Submissions.Where(s => s.UserId == userId))
            .ToListAsync();

        var idx = assignments.FindIndex(a => a.Id == assignmentId);
        if (idx <= 0) return;

        var prev = assignments[idx - 1];
        var prevSubmission = prev.Submissions.FirstOrDefault(s => s.UserId == userId);
        if (prevSubmission?.Grade == null || prevSubmission.Grade < 5)
            throw new UnauthorizedAccessException("Complete the previous assignment to unlock this assignment.");
    }

    // ── Private Helpers ───────────────────────────────────────

    private async Task<Entities.Classroom> GetOwnedClassroomAsync(int id, string userId)
    {
        var classroom = await _context.Classrooms
            .Include(c => c.CreatedBy)
            .Include(c => c.LearningPath)
            .Include(c => c.UserClassrooms)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("Classroom not found.");

        if (classroom.CreatedById != userId)
            throw new UnauthorizedAccessException("You do not own this classroom.");

        return classroom;
    }

    private async Task EnsureInstructorAsync(int classroomId, string userId)
    {
        var membership = await _context.UserClassrooms
            .FirstOrDefaultAsync(uc => uc.ClassroomId == classroomId && uc.UserId == userId)
            ?? throw new UnauthorizedAccessException("Not a member.");

        if (membership.Role != "Instructor")
            throw new UnauthorizedAccessException("Instructor access required.");
    }

    private async Task EnsureAdminAsync(int classroomId, string userId)
    {
        var isMember = await _context.UserClassrooms
            .AnyAsync(uc => uc.ClassroomId == classroomId && uc.UserId == userId);
        if (!isMember) throw new UnauthorizedAccessException("Not a member.");

        var adminRoleId = await _context.Roles
            .Where(r => r.Name == "Admin")
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        var isAdmin = adminRoleId != null && await _context.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == adminRoleId);

        if (!isAdmin)
            throw new UnauthorizedAccessException("Admin access required.");
    }

    private async Task<Assignment> GetAssignmentAsync(int assignmentId, int classroomId)
    {
        return await _context.Assignments
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.ClassroomId == classroomId)
            ?? throw new KeyNotFoundException("Assignment not found.");
    }

    private static string GenerateInviteCode() =>
        Guid.NewGuid().ToString("N")[..8].ToUpper();

    private static ClassroomResponseDto MapToResponse(
        Entities.Classroom c, string userRole, string userId) => new()
        {
            Id = c.Id,
            Title = c.Title,
            Description = c.Description,
            InviteCode = c.InviteCode,
            LearningPathId = c.LearningPathId,
            LearningPathTitle = c.LearningPath?.Title ?? string.Empty,
            CreatedById = c.CreatedById,
            CreatedByName = c.CreatedBy is not null
            ? $"{c.CreatedBy.FirstName} {c.CreatedBy.LastName}" : string.Empty,
            MemberCount = c.UserClassrooms?.Count ?? 0,
            UserRole = userRole,
            CreatedAt = c.CreatedAt,
        };
}