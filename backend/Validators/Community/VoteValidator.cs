using FluentValidation;
using LearnPath.API.DTOs.Community;

namespace LearnPath.API.Validators.Community;

public class VoteValidator : AbstractValidator<VoteDto>
{
    public VoteValidator()
    {
        RuleFor(x => x.IsUpvote)
            .NotNull().WithMessage("isUpvote is required (true for upvote, false for downvote).");
    }
}
