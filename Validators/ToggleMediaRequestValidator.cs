using ChatServer.Core.Models;
using ChatServer.Core.Constants;
using FluentValidation;

namespace ChatServer.Validators;

public class ToggleMediaRequestValidator : AbstractValidator<ToggleMediaRequest>
{
    public ToggleMediaRequestValidator()
    {
        RuleFor(x => x.media_type)
            .NotEmpty()
            .Must(type => type == GroupCallConstants.CallType.Audio || type == GroupCallConstants.CallType.Video)
            .WithMessage("Media type must be either 'audio' or 'video'");
    }
}