using FluentValidation;
using LearnPath.API.DTOs.Community;

namespace LearnPath.API.Validators.Community;

public class CreatePostValidator : AbstractValidator<CreatePostDto>
{
    public CreatePostValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(300).WithMessage("Title cannot exceed 300 characters.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .MaximumLength(10000).WithMessage("Content cannot exceed 10000 characters.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.")
            .MaximumLength(100).WithMessage("Category cannot exceed 100 characters.");

        RuleFor(x => x.CodeSnippet)
            .MaximumLength(50000).WithMessage("Code snippet cannot exceed 50000 characters.");

        RuleFor(x => x.ProgrammingLanguage)
            .NotEmpty().WithMessage("Programming language is required when a code snippet is provided.")
            .When(x => !string.IsNullOrWhiteSpace(x.CodeSnippet))
            .MaximumLength(100).WithMessage("Programming language cannot exceed 100 characters.");

        RuleFor(x => x.Tags)
            .MaximumLength(500).WithMessage("Tags cannot exceed 500 characters.");

        RuleFor(x => x.LearningPathId)
            .GreaterThan(0).When(x => x.LearningPathId.HasValue)
            .WithMessage("Learning path id must be a positive number.");
    }
}
