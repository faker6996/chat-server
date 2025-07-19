using ChatServer.Models;
using FluentValidation;

namespace ChatServer.Validators
{
    public class CreateGroupRequestValidator : AbstractValidator<CreateGroupRequest>
    {
        public CreateGroupRequestValidator()
        {
            RuleFor(x => x.name)
                .NotEmpty()
                .WithMessage("Group name is required")
                .Length(1, 100)
                .WithMessage("Group name must be between 1 and 100 characters");

            RuleFor(x => x.description)
                .MaximumLength(500)
                .WithMessage("Description must not exceed 500 characters");

            RuleFor(x => x.initial_members)
                .NotEmpty()
                .WithMessage("At least one initial member is required");

            RuleFor(x => x.max_members)
                .GreaterThan(0)
                .LessThanOrEqualTo(1000)
                .WithMessage("Max members must be between 1 and 1000");
        }
    }
}