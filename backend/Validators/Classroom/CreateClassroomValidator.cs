using FluentValidation;
using LearnPath.API.DTOs.Classroom;

namespace LearnPath.API.Validators.Classroom;

public class CreateClassroomValidator : AbstractValidator<CreateClassroomDto>
{
    public CreateClassroomValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(1000);

        RuleFor(x => x.LearningPathId)
            .GreaterThan(0).WithMessage("A valid learning path must be selected.");
    }
}
