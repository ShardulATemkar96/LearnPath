using AutoMapper;
using LearnPath.API.DTOs.Quiz;
using LearnPath.API.Entities;

namespace LearnPath.API.Mappings;

public class QuizMappingProfile : Profile
{
    public QuizMappingProfile()
    {
        // Create → Entity
        CreateMap<CreateQuizDto, Quiz>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ModuleId, opt => opt.Ignore())
            .ForMember(dest => dest.IsPublished, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Module, opt => opt.Ignore())
            .ForMember(dest => dest.Questions, opt => opt.Ignore())
            .ForMember(dest => dest.Attempts, opt => opt.Ignore());

        CreateMap<CreateQuestionDto, QuizQuestion>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.QuizId, opt => opt.Ignore())
            .ForMember(dest => dest.Quiz, opt => opt.Ignore())
            .ForMember(dest => dest.Options, opt => opt.Ignore())
            .ForMember(dest => dest.Answers, opt => opt.Ignore());

        CreateMap<CreateOptionDto, QuizOption>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.QuizQuestionId, opt => opt.Ignore())
            .ForMember(dest => dest.Question, opt => opt.Ignore());

        // Quiz → Admin response (with questions)
        CreateMap<Quiz, QuizResponseDto>()
            .ForMember(dest => dest.QuestionCount, opt => opt.MapFrom(src => src.Questions.Count))
            .ForMember(dest => dest.Questions, opt => opt.MapFrom(src => src.Questions.OrderBy(q => q.OrderIndex)));

        // Question → Admin response (with IsCorrect in options)
        CreateMap<QuizQuestion, AdminQuestionResponseDto>()
            .ForMember(dest => dest.Options, opt => opt.MapFrom(src => src.Options.OrderBy(o => o.OrderIndex)));

        CreateMap<QuizOption, AdminOptionResponseDto>();

        // Question → learner attempt question (NO IsCorrect)
        CreateMap<QuizQuestion, AttemptQuestionDto>()
            .ForMember(dest => dest.QuestionId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Options, opt => opt.MapFrom(src => src.Options.OrderBy(o => o.OrderIndex)));

        CreateMap<QuizOption, AttemptOptionDto>();

        // Question → learner response (for results)
        CreateMap<QuizQuestion, QuizQuestionResponseDto>()
            .ForMember(dest => dest.Options, opt => opt.MapFrom(src => src.Options.OrderBy(o => o.OrderIndex)));

        CreateMap<QuizOption, QuizOptionResponseDto>();

        // AttemptSummary
        CreateMap<QuizAttempt, AttemptSummaryDto>()
            .ForMember(dest => dest.AttemptId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Score, opt => opt.MapFrom(src => src.Score ?? 0))
            .ForMember(dest => dest.TotalPoints, opt => opt.MapFrom(src => src.TotalPoints ?? 0))
            .ForMember(dest => dest.IsPassed, opt => opt.MapFrom(src => src.IsPassed ?? false))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
    }
}
