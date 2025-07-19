using ChatServer.Core.Models;
using FluentValidation;

namespace ChatServer.Validators
{
    public class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
    {
        public SendMessageRequestValidator()
        {
            RuleFor(x => x.sender_id)
                .GreaterThan(0)
                .WithMessage("Sender ID must be greater than 0");

            // Conversation ID bắt buộc cho tin nhắn nhóm, optional cho tin nhắn cá nhân
            RuleFor(x => x.conversation_id)
                .GreaterThan(0)
                .When(x => x.conversation_id.HasValue)
                .WithMessage("Conversation ID must be greater than 0 when provided");

            // Với tin nhắn nhóm: bắt buộc phải có conversation_id
            RuleFor(x => x.conversation_id)
                .NotEmpty()
                .When(x => x.message_type == ChatServer.Core.Constants.MESSAGE_TYPE.GROUP)
                .WithMessage("Conversation ID is required for group messages");

            // Khi không có conversation_id thì bắt buộc phải có target_id
            RuleFor(x => x.target_id)
                .NotEmpty()
                .When(x => !x.conversation_id.HasValue)
                .WithMessage("Target ID is required when conversation ID is not provided");

            RuleFor(x => x.content)
                .NotEmpty()
                .When(x => x.attachments == null || x.attachments.Count == 0)
                .WithMessage("Content is required when no attachments are provided");

            RuleFor(x => x.content)
                .MaximumLength(4000)
                .WithMessage("Content must not exceed 4000 characters");

            RuleFor(x => x.message_type)
                .NotEmpty()
                .WithMessage("Message type is required");

            RuleFor(x => x.content_type)
                .NotEmpty()
                .WithMessage("Content type is required");

            RuleFor(x => x.attachments)
                .Must(x => x == null || x.Count <= 10)
                .WithMessage("Maximum 10 attachments allowed");
        }
    }
}