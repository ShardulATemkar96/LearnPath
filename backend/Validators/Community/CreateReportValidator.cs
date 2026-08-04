using FluentValidation;
using LearnPath.API.DTOs.Community;

namespace LearnPath.API.Validators.Community;

public class CreateReportValidator : AbstractValidator<CreateReportDto>
{
    public CreateReportValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A reason is required.")
            .MaximumLength(1000).WithMessage("Reason cannot exceed 1000 characters.");
    }
}
