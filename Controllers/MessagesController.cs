// Sửa file Controllers/MessagesController.cs

using ChatServer.Constants;
using ChatServer.Models;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace ChatServer.Controllers;

public class MessagesController : BaseApiController
{
    private readonly IConnection _conn;
    private readonly ILogger<MessagesController> _logger;

    private const string TopicExchange = "chat_topic_exchange";

    public MessagesController(IConnection conn, ILogger<MessagesController> logger)
    {
        _conn = conn;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> PostMessage([FromBody] SendMessageRequest req)
    {
        // 1️⃣ Validate TargetId
        if ((req.message_type is MESSAGE_TYPE.PRIVATE or MESSAGE_TYPE.GROUP) &&
            !req.target_id.HasValue)
        {
            // SỬ DỤNG HELPER MỚI
            return BadRequestResponse("TargetId is required for private or group messages.");
        }

        // 2️⃣ Tạo channel
        await using var ch = await _conn.CreateChannelAsync();

        // 3️⃣ Routing key
        var routingKey = req.message_type switch
        {
            MESSAGE_TYPE.PRIVATE => $"chat.private.{req.target_id}",
            MESSAGE_TYPE.GROUP => $"chat.group.{req.target_id}",
            _ => "chat.public.all"
        };

        // 4️⃣ Đóng gói payload
        var msg = new ChatMessage
        {
            sender_id = req.sender_id,
            content = req.content,
            timestamp = DateTime.UtcNow,
            message_type = req.message_type,
            target_id = req.target_id,
            created_at = DateTime.UtcNow,
            status = MessageStatus.Sent
        };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg));

        try
        {
            // 5️⃣ Publish
            await ch.BasicPublishAsync(
                    exchange: TopicExchange,
                    routingKey: routingKey,
                    body: body);

            // SỬ DỤNG HELPER MỚI
            // Dữ liệu trả về là một object ẩn danh, được bọc trong lớp ApiResponse
            var responseData = new { Status = "Message routed successfully", RoutingKey = routingKey };
            return OkResponse(responseData, "Message published successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ publish failed for routing key {routingKey}", routingKey);

            // SỬ DỤNG HELPER MỚI
            return InternalErrorResponse("Failed to publish message due to a server error.");
        }
    }

}