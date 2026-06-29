using LearnPath.API.Data;
using LearnPath.API.DTOs.Classroom;
using LearnPath.API.Entities;
using LearnPath.API.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace LearnPath.API.Services.Classroom;

public class ClassroomService : IClassroomService
{
    private readonly ApplicationDbContext _context;

    public ClassroomService(ApplicationDbContext context)
    {
        _context = context;
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
            JoinedAt = uc.JoinedAt,
        }).ToList();

        var assignments = classroom.Assignments.Select(a => new AssignmentResponseDto
        {
            Id = a.Id,
            Title = a.Title,
            Description = a.Description,
            DueDate = a.DueDate,
            ClassroomId = a.ClassroomId,
            SubmissionCount = a.Submissions.Count,
            HasSubmitted = a.Submissions.Any(s => s.UserId == userId),
            CreatedAt = a.CreatedAt,
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

        await _context.Entry(classroom).Reference(c => c.CreatedBy).LoadAsync();
        await _context.Entry(classroom).Reference(c => c.LearningPath).LoadAsync();
        await _context.Entry(classroom).Collection(c => c.UserClassrooms).LoadAsync();

        return MapToResponse(classroom, "Instructor", userId);
    }

    public async Task<ClassroomResponseDto> UpdateAsync(
        int id, UpdateClassroomDto dto, string userId)
    {
        var classroom = await GetOwnedClassroomAsync(id, userId);
        classroom.Title = dto.Title;
        classroom.Description = dto.Description;
        classroom.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
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
                JoinedAt = uc.JoinedAt,
            })
            .ToListAsync();
    }

    // ── Assignments ───────────────────────────────────────────

    public async Task<AssignmentResponseDto> CreateAssignmentAsync(
        int classroomId, CreateAssignmentDto dto, string userId)
    {
        await EnsureInstructorAsync(classroomId, userId);

        var assignment = new Assignment
        {
            Title = dto.Title,
            Description = dto.Description,
            DueDate = dto.DueDate,
            ClassroomId = classroomId,
        };

        await _context.Assignments.AddAsync(assignment);
        await _context.SaveChangesAsync();

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
    }

    // ── Submissions ───────────────────────────────────────────

    public async Task<SubmissionResponseDto> SubmitAssignmentAsync(
        int classroomId, int assignmentId, CreateSubmissionDto dto, string userId)
    {
        var isMember = await _context.UserClassrooms
            .AnyAsync(uc => uc.ClassroomId == classroomId && uc.UserId == userId);
        if (!isMember) throw new UnauthorizedAccessException("Not a member.");

        var assignment = await GetAssignmentAsync(assignmentId, classroomId);

        var already = await _context.Submissions
            .AnyAsync(s => s.AssignmentId == assignmentId && s.UserId == userId);
        if (already) throw new ArgumentException("Already submitted.");

        var submission = new Submission
        {
            AssignmentId = assignmentId,
            UserId = userId,
            ContentUrl = dto.ContentUrl,
        };

        await _context.Submissions.AddAsync(submission);
        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(userId);

        return new SubmissionResponseDto
        {
            Id = submission.Id,
            AssignmentId = submission.AssignmentId,
            UserId = submission.UserId,
            UserFullName = $"{user!.FirstName} {user.LastName}",
            ContentUrl = submission.ContentUrl,
            SubmittedAt = submission.SubmittedAt,
        };
    }

    public async Task<List<SubmissionResponseDto>> GetSubmissionsAsync(
        int classroomId, int assignmentId, string userId)
    {
        await EnsureInstructorAsync(classroomId, userId);

        return await _context.Submissions
            .Where(s => s.AssignmentId == assignmentId)
            .Include(s => s.User)
            .Select(s => new SubmissionResponseDto
            {
                Id = s.Id,
                AssignmentId = s.AssignmentId,
                UserId = s.UserId,
                UserFullName = $"{s.User.FirstName} {s.User.LastName}",
                ContentUrl = s.ContentUrl,
                Feedback = s.Feedback,
                Grade = s.Grade,
                SubmittedAt = s.SubmittedAt,
            })
            .ToListAsync();
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
        submission.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new SubmissionResponseDto
        {
            Id = submission.Id,
            AssignmentId = submission.AssignmentId,
            UserId = submission.UserId,
            UserFullName = $"{submission.User.FirstName} {submission.User.LastName}",
            ContentUrl = submission.ContentUrl,
            Feedback = submission.Feedback,
            Grade = submission.Grade,
            SubmittedAt = submission.SubmittedAt,
        };
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