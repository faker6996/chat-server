// File: Application/MessageService.cs
using ChatServer.Core.Constants;
using ChatServer.Core.Models;
using ChatServer.Infrastructure.Repositories.Messenger;
using ChatServer.Infrastructure.Repositories.Attachment;
using Microsoft.Extensions.Logging; // Thêm using cho ILogger

// Sửa lại namespace cho nhất quán
namespace ChatServer.Infrastructure.Services;

public class MessageService : IMessageService
{
    private readonly IMessagePublisher _messagePublisher;
    private readonly IMessageRepo _messageRepo;
    private readonly IAttachmentRepo _attachmentRepo;
    private readonly ILogger<MessageService> _logger;

    private const string TopicExchange = "chat_topic_exchange";

    public MessageService(IMessagePublisher messagePublisher, ILogger<MessageService> logger, IMessageRepo messageRepo, IAttachmentRepo attachmentRepo)
    {
        _messagePublisher = messagePublisher;
        _logger = logger;
        _messageRepo = messageRepo;
        _attachmentRepo = attachmentRepo;
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

        // 3️⃣ Xác định content_type dựa trên attachments
        var contentType = request.content_type;
        if (request.attachments != null && request.attachments.Count > 0)
        {
            // Nếu có attachments, kiểm tra loại file
            var firstAttachment = request.attachments[0];
            if (firstAttachment.file_type.StartsWith("image/"))
            {
                contentType = "image";
            }
            else
            {
                contentType = "file";
            }
        }

        // 4️⃣ Tạo đối tượng Message
        var msg = new Message
        {
            sender_id = request.sender_id,
            content = request.content,
            conversation_id = request.conversation_id,
            message_type = request.message_type,
            content_type = contentType,
            target_id = request.target_id,
            reply_to_message_id = request.reply_to_message_id,
            created_at = DateTime.UtcNow,
            status = MessageStatus.Sent.ToString()
        };

        try
        {
            // 5️⃣ Lưu message vào database trước để có ID
            var savedMessage = await _messageRepo.InsertMessageAsync(msg);

            // 6️⃣ Nếu có attachments, lưu vào database
            if (request.attachments != null && request.attachments.Count > 0)
            {
                foreach (var attachmentReq in request.attachments)
                {
                    var attachment = new Attachment
                    {
                        message_id = savedMessage.id,
                        file_name = attachmentReq.file_name,
                        file_url = attachmentReq.file_url,
                        file_type = attachmentReq.file_type,
                        file_size = attachmentReq.file_size,
                        created_at = DateTime.UtcNow
                    };
                    await _attachmentRepo.CreateAsync(attachment);
                }
            }

            // 7️⃣ Gọi đến Publisher để gửi tin
            await _messagePublisher.PublishAsync(TopicExchange, routingKey, savedMessage);

            // 8️⃣ Trả về kết quả thành công
            var responseData = new { Status = "Message routed successfully", RoutingKey = routingKey };
            return new MessageServiceResult(true, data: savedMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while publishing the message.");
            return new MessageServiceResult(false, "Failed to publish message due to a server error.");
        }
    }

    public async Task<MessageServiceResult> GetSyncMessages(int conId, int mesLastId)
    {
        var listMes = await _messageRepo.GetMessagesAfterIdAsync(conId, mesLastId);

        // Tạo MessageResponse với attachments
        var messageResponses = new List<MessageResponse>();
        foreach (var message in listMes)
        {
            var attachments = await _attachmentRepo.GetByMessageIdAsync(message.id);
            var messageResponse = new MessageResponse
            {
                id = message.id,
                conversation_id = message.conversation_id,
                sender_id = message.sender_id,
                content = message.content,
                message_type = message.message_type,
                content_type = message.content_type,
                created_at = message.created_at,
                status = message.status,
                target_id = message.target_id,
                attachments = attachments.Count > 0 ? attachments : null
            };
            messageResponses.Add(messageResponse);
        }

        return new MessageServiceResult(true, data: messageResponses);
    }
}