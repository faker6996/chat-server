using ChatServer.Constants;
using ChatServer.Models;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace ChatServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
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
    public async Task<IActionResult> PostMessage(
        [FromBody] SendMessageRequest req,
        CancellationToken ct)          // vẫn nhận token, nhưng không truyền cho RabbitMQ
    {
        // 1️⃣ Validate TargetId
        if ((req.MessageType is MESSAGE_TYPE.PRIVATE or MESSAGE_TYPE.GROUP) &&
            req.TargetId.HasValue == false)
        {
            return BadRequest("TargetId is required for private or group messages.");
        }

        // 2️⃣ Tạo channel. Không dùng ct (request-aborted) để tránh cancel giữa chừng.
        await using var ch = await _conn.CreateChannelAsync();

        // ⚠️  Exchange/Queue đã được declare 1 lần ở Startup (xem chú thích dưới),
        //     nên KHÔNG gọi ExchangeDeclareAsync ở đây nữa.

        // 3️⃣ Routing key
        var routingKey = req.MessageType switch
        {
            MESSAGE_TYPE.PRIVATE => $"chat.private.{req.TargetId}",
            MESSAGE_TYPE.GROUP => $"chat.group.{req.TargetId}",
            _ => "chat.public.all"
        };

        // 4️⃣ Đóng gói payload
        var msg = new ChatMessage
        {
            User = req.SenderId,
            Content = req.Content,
            Timestamp = DateTime.UtcNow,
            MessageType = req.MessageType,
            TargetId = req.TargetId
        };
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg));

        try
        {
            // 5️⃣ Publish – không chuyền RequestAborted
            await ch.BasicPublishAsync(
                    exchange: TopicExchange,
                    routingKey: routingKey,
                    body: body,
                    mandatory: false,
                    cancellationToken: CancellationToken.None);

            return Ok(new { status = "Message routed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ publish failed");
            // 500 cho lỗi server thực sự (khác 499 client abort)
            return StatusCode(500, "Failed to route message");
        }
    }
}
