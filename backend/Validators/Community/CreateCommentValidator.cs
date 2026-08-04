using FluentValidation;
using LearnPath.API.DTOs.Community;

namespace LearnPath.API.Validators.Community;

public class CreateCommentValidator : AbstractValidator<CreateCommentDto>
{
    public CreateCommentValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Comment content is required.")
            .MaximumLength(5000).WithMessage("Comment cannot exceed 5000 characters.");

        RuleFor(x => x.ParentCommentId)
            .GreaterThan(0).When(x => x.ParentCommentId.HasValue)
            .WithMessage("Parent comment id must be a positive number.");
    }
}
