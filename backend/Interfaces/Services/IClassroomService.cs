using LearnPath.API.DTOs.Classroom;

namespace LearnPath.API.Interfaces.Services;

public interface IClassroomService
{
    Task<List<ClassroomResponseDto>> GetMyClassroomsAsync(string userId);
    Task<ClassroomDetailResponseDto> GetByIdAsync(int id, string userId);
    Task<ClassroomResponseDto> CreateAsync(CreateClassroomDto dto, string userId);
    Task<ClassroomResponseDto> UpdateAsync(int id, UpdateClassroomDto dto, string userId);
    Task DeleteAsync(int id, string userId);
    Task JoinAsync(string inviteCode, string userId);
    Task LeaveAsync(int id, string userId);
    Task<List<ClassroomMemberDto>> GetMembersAsync(int id, string userId);

    Task<AssignmentResponseDto> CreateAssignmentAsync(int classroomId, CreateAssignmentDto dto, string userId);
    Task<AssignmentResponseDto> UpdateAssignmentAsync(int classroomId, int assignmentId, UpdateAssignmentDto dto, string userId);
    Task DeleteAssignmentAsync(int classroomId, int assignmentId, string userId);

    Task<SubmissionResponseDto> SubmitAssignmentAsync(int classroomId, int assignmentId, CreateSubmissionDto dto, string userId);
    Task<List<SubmissionResponseDto>> GetSubmissionsAsync(int classroomId, int assignmentId, string userId);
    Task<SubmissionResponseDto> GradeSubmissionAsync(int classroomId, int assignmentId, int submissionId, GradeSubmissionDto dto, string userId);
}
