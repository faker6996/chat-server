using ChatServer.Core.Models;
using ChatServer.Core.Constants;
using FluentValidation;

namespace ChatServer.Validators;

public class StartGroupCallRequestValidator : AbstractValidator<StartGroupCallRequest>
{
    public StartGroupCallRequestValidator()
    {
        RuleFor(x => x.call_type)
            .NotEmpty()
            .Must(type => type == GroupCallConstants.CallType.Video || type == GroupCallConstants.CallType.Audio)
            .WithMessage("Call type must be either 'video' or 'audio'");

        RuleFor(x => x.max_participants)
            .GreaterThan(0)
            .LessThanOrEqualTo(50)
            .WithMessage("Max participants must be between 1 and 50")
            .When(x => x.max_participants.HasValue);

        RuleFor(x => x.invite_user_ids)
            .Must(list => list == null || list.Count <= 50)
            .WithMessage("Cannot invite more than 50 users")
            .When(x => x.invite_user_ids != null);
    }
}