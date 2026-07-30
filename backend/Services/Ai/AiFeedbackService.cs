using LearnPath.API.Configuration;
using LearnPath.API.Data;
using LearnPath.API.DTOs.Ai;
using LearnPath.API.Entities;
using LearnPath.API.Interfaces.Services;
using LearnPath.API.Interfaces.Services.Ai;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LearnPath.API.Services.Ai;

public class AiFeedbackService : IAiFeedbackService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;
    private readonly IAiProvider _aiProvider;
    private readonly PromptBuilder _promptBuilder;
    private readonly AiResponseParser _responseParser;
    private readonly AiOptions _options;
    private readonly ILogger<AiFeedbackService> _logger;

    public AiFeedbackService(
        ApplicationDbContext context,
        IFileStorageService fileStorage,
        IAiProvider aiProvider,
        PromptBuilder promptBuilder,
        AiResponseParser responseParser,
        IOptions<AiOptions> options,
        ILogger<AiFeedbackService> logger)
    {
        _context = context;
        _fileStorage = fileStorage;
        _aiProvider = aiProvider;
        _promptBuilder = promptBuilder;
        _responseParser = responseParser;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AiFeedbackResponseDto> GenerateAsync(int submissionId, string userId)
    {
        var submission = await _context.Submissions
            .Include(s => s.Assignment)
                .ThenInclude(a => a.Classroom)
            .FirstOrDefaultAsync(s => s.Id == submissionId)
            ?? throw new KeyNotFoundException("Submission not found.");

        var isInstructor = await _context.UserClassrooms
            .AnyAsync(uc => uc.ClassroomId == submission.Assignment.ClassroomId
                         && uc.UserId == userId
                         && uc.Role == "Instructor");

        if (!isInstructor)
            throw new UnauthorizedAccessException("Only instructors can generate AI feedback.");

        var isRegeneration = await _context.SubmissionAiFeedbacks
            .AnyAsync(f => f.SubmissionId == submissionId);

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("AI Feedback is unavailable. The API key is not configured.");

        _logger.LogInformation(
            "AI request {Action} for submission {SubmissionId}",
            isRegeneration ? "regeneration" : "generation",
            submissionId);

        var submissionText = await ReadSubmissionTextAsync(submission);

        if (string.IsNullOrWhiteSpace(submissionText))
            throw new InvalidOperationException("AI Feedback cannot be generated. The submission contains no extractable text.");

        var request = new AiFeedbackRequestDto
        {
            SubmissionId = submission.Id,
            AssignmentQuestion = submission.Assignment.Title,
            AssignmentDescription = submission.Assignment.Description,
            AssignmentRubric = string.Empty,
            StudentSubmission = submissionText,
        };

        var prompt = _promptBuilder.BuildPrompt(request);

        var providerResponse = await _aiProvider.SendAsync(
            new AiProviderRequest { Prompt = prompt, TimeoutSeconds = _options.TimeoutSeconds });

        if (!providerResponse.Success)
        {
            _logger.LogWarning(
                "AI request failed for submission {SubmissionId}: {ErrorMessage}",
                submissionId,
                providerResponse.ErrorMessage);
            throw new InvalidOperationException(providerResponse.ErrorMessage);
        }

        var feedback = _responseParser.Parse(providerResponse.Content);

        if (string.IsNullOrWhiteSpace(feedback.Summary))
        {
            _logger.LogWarning(
                "AI returned incomplete response for submission {SubmissionId}. Treating as failure.",
                submissionId);
            throw new InvalidOperationException("AI returned an incomplete response. Please try again.");
        }

        var existing = await _context.SubmissionAiFeedbacks
            .FirstOrDefaultAsync(f => f.SubmissionId == submissionId);

        if (existing is not null)
        {
            existing.Summary = feedback.Summary;
            existing.GrammarFeedback = feedback.GrammarFeedback;
            existing.RubricCoverage = feedback.RubricCoverage;
            existing.MissingTopics = feedback.MissingTopics;
            existing.SuggestedScorePercentage = feedback.SuggestedScore.Percentage;
            existing.SuggestedScoreMarks = feedback.SuggestedScore.Marks;
            existing.OverallRecommendation = feedback.OverallRecommendation;
            existing.Disclaimer = feedback.Disclaimer;
            existing.RawResponse = providerResponse.Content;
            existing.GeneratedAt = DateTime.UtcNow;
        }
        else
        {
            var entity = new SubmissionAiFeedback
            {
                SubmissionId = submissionId,
                Summary = feedback.Summary,
                GrammarFeedback = feedback.GrammarFeedback,
                RubricCoverage = feedback.RubricCoverage,
                MissingTopics = feedback.MissingTopics,
                SuggestedScorePercentage = feedback.SuggestedScore.Percentage,
                SuggestedScoreMarks = feedback.SuggestedScore.Marks,
                OverallRecommendation = feedback.OverallRecommendation,
                Disclaimer = feedback.Disclaimer,
                RawResponse = providerResponse.Content,
                GeneratedAt = DateTime.UtcNow,
            };
            _context.SubmissionAiFeedbacks.Add(entity);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "AI request completed for submission {SubmissionId}",
            submissionId);

        return feedback;
    }

    public async Task<AiFeedbackResponseDto?> GetAsync(int submissionId, string userId)
    {
        var submission = await _context.Submissions
            .Include(s => s.Assignment)
                .ThenInclude(a => a.Classroom)
            .FirstOrDefaultAsync(s => s.Id == submissionId)
            ?? throw new KeyNotFoundException("Submission not found.");

        var isInstructor = await _context.UserClassrooms
            .AnyAsync(uc => uc.ClassroomId == submission.Assignment.ClassroomId
                         && uc.UserId == userId
                         && uc.Role == "Instructor");

        if (!isInstructor)
            throw new UnauthorizedAccessException("Only instructors can view AI feedback.");

        var entity = await _context.SubmissionAiFeedbacks
            .FirstOrDefaultAsync(f => f.SubmissionId == submissionId);

        if (entity is null) return null;

        return new AiFeedbackResponseDto
        {
            Summary = entity.Summary,
            GrammarFeedback = entity.GrammarFeedback,
            RubricCoverage = entity.RubricCoverage,
            MissingTopics = entity.MissingTopics,
            SuggestedScore = new SuggestedScoreDto
            {
                Percentage = entity.SuggestedScorePercentage,
                Marks = entity.SuggestedScoreMarks,
            },
            OverallRecommendation = entity.OverallRecommendation,
            Disclaimer = entity.Disclaimer,
            GeneratedAt = entity.GeneratedAt,
        };
    }

    private async Task<string> ReadSubmissionTextAsync(Entities.Submission submission)
    {
        if (string.IsNullOrWhiteSpace(submission.StoredFilePath))
            throw new InvalidOperationException("Submission has no file.");

        var ext = submission.FileExtension?.ToLowerInvariant();

        if (ext is "txt" or ".txt")
        {
            var stream = await _fileStorage.GetStreamAsync(submission.StoredFilePath);
            if (stream is null)
                throw new InvalidOperationException("Submission file not found on disk.");

            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        if (ext is "pdf" or ".pdf")
        {
            var stream = await _fileStorage.GetStreamAsync(submission.StoredFilePath);
            if (stream is null)
                throw new InvalidOperationException("Submission file not found on disk.");

            using var pdf = UglyToad.PdfPig.PdfDocument.Open(stream);
            var text = string.Join("\n", pdf.GetPages().Select(p => p.Text));
            return string.IsNullOrWhiteSpace(text)
                ? string.Empty
                : text;
        }

        throw new InvalidOperationException(
            $"AI feedback is not supported for {ext} files. " +
            "Please upload a TXT or PDF file for AI analysis.");
    }
}
