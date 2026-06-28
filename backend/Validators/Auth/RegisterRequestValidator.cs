using FluentValidation;
using LearnPath.API.DTOs.Auth;

namespace LearnPath.API.Validators.Auth;

public class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Enter a valid email.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Minimum 8 characters.")
            .Matches("[A-Z]").WithMessage("Must contain an uppercase letter.")
            .Matches("[0-9]").WithMessage("Must contain a number.");
    }
}
