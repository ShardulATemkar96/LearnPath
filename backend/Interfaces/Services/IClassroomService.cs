using LearnPath.API.DTOs.Classroom;
using Microsoft.AspNetCore.Http;

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
    Task RemoveMemberAsync(int classroomId, string memberUserId, string userId);
    Task<List<ClassroomMemberDto>> GetMembersAsync(int id, string userId);

    Task<AssignmentResponseDto> CreateAssignmentAsync(int classroomId, CreateAssignmentDto dto, string userId);
    Task<AssignmentResponseDto> UpdateAssignmentAsync(int classroomId, int assignmentId, UpdateAssignmentDto dto, string userId);
    Task DeleteAssignmentAsync(int classroomId, int assignmentId, string userId);

    Task<SubmissionResponseDto> UploadSubmissionAsync(int classroomId, int assignmentId, IFormFile file, string userId);
    Task<SubmissionResponseDto> GetMySubmissionAsync(int classroomId, int assignmentId, string userId);
    Task DeleteSubmissionAsync(int classroomId, int assignmentId, string userId);
    Task<List<SubmissionResponseDto>> GetSubmissionsAsync(int classroomId, int assignmentId, string userId);
    Task<SubmissionResponseDto> GradeSubmissionAsync(int classroomId, int assignmentId, int submissionId, GradeSubmissionDto dto, string userId);
    Task<SubmissionResponseDto> VerifySubmissionAsync(int classroomId, int assignmentId, int submissionId, string userId);
    Task<SubmissionResponseDto> CompleteSubmissionAsync(int classroomId, int assignmentId, int submissionId, string userId);
    Task<SubmissionResponseDto> GetSubmissionByIdAsync(int submissionId, string userId);
    Task<(Stream? FileStream, string? FileName, string? MimeType)> GetPreviewFileAsync(int submissionId, string userId);
    Task<(Stream? FileStream, string? FileName, string? MimeType)> GetDownloadFileAsync(int submissionId, string userId);
    Task<SubmissionResponseDto> TransitionStatusAsync(int submissionId, TransitionStatusDto dto, string userId);
    Task<SubmissionResponseDto> ReturnForResubmissionAsync(int submissionId, ReturnSubmissionDto dto, string userId);
    Task<SubmissionResponseDto> PublishEvaluationAsync(int classroomId, int assignmentId, int submissionId, string userId);
}
