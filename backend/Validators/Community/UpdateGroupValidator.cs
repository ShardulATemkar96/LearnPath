using FluentValidation;
using LearnPath.API.DTOs.Community;

namespace LearnPath.API.Validators.Community;

public class UpdateGroupValidator : AbstractValidator<UpdateGroupDto>
{
    public UpdateGroupValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Group name is required.")
            .MaximumLength(100).WithMessage("Group name cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Group description is required.")
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");
    }
}
