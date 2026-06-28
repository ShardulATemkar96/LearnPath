using FluentValidation;
using LearnPath.API.DTOs.LearningPath;

namespace LearnPath.API.Validators.LearningPath;

public class CreateLearningPathValidator : AbstractValidator<CreateLearningPathDto>
{
    public CreateLearningPathValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(2000);
    }
}
