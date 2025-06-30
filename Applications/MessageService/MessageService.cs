// File: Application/MessageService.cs
using ChatServer.Constants;
using ChatServer.Models;
using Microsoft.Extensions.Logging; // Thêm using cho ILogger

// Sửa lại namespace cho nhất quán
namespace ChatServer.Applications;

public class MessageService : IMessageService
{
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<MessageService> _logger;

    private const string TopicExchange = "chat_topic_exchange";

    public MessageService(IMessagePublisher messagePublisher, ILogger<MessageService> logger)
    {
        _messagePublisher = messagePublisher;
        _logger = logger;
    }

    public async Task<MessageServiceResult> SendMessageAsync(SendMessageRequest request)
    {
        // 1️⃣ Validate nghiệp vụ
        if ((request.message_type is MESSAGE_TYPE.PRIVATE or MESSAGE_TYPE.GROUP) &&
            !request.target_id.HasValue)
        {
            return new MessageServiceResult(false, "TargetId is required for private or group messages.");
        }

        // 2️⃣ Xử lý logic tạo Routing key
        var routingKey = request.message_type switch
        {
            MESSAGE_TYPE.PRIVATE => $"chat.private.{request.target_id}",
            MESSAGE_TYPE.GROUP => $"chat.group.{request.target_id}",
            _ => "chat.public.all"
        };

        // 3️⃣ Tạo đối tượng Message
        var msg = new Message
        {
            sender_id = request.sender_id,
            content = request.content,
            conversation_id = request.conversation_id,
            message_type = request.message_type,
            target_id = request.target_id,
            created_at = DateTime.UtcNow,
            status = MessageStatus.Sent.ToString()
        };

        try
        {
            // 4️⃣ Gọi đến Publisher để gửi tin
            await _messagePublisher.PublishAsync(TopicExchange, routingKey, msg);

            // 5️⃣ Trả về kết quả thành công
            var responseData = new { Status = "Message routed successfully", RoutingKey = routingKey };
            return new MessageServiceResult(true, data: msg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while publishing the message.");
            return new MessageServiceResult(false, "Failed to publish message due to a server error.");
        }
    }
}