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
    private const string TopicExchange = "chat_topic_exchange";

    public MessagesController(IConnection conn)
    {
        _conn = conn;
    }

    [HttpPost]
    public async Task<IActionResult> PostMessage([FromBody] SendMessageRequest req,
                                                 CancellationToken ct)
    {
        if ((req.MessageType is MESSAGE_TYPE.PRIVATE or MESSAGE_TYPE.GROUP)
            && string.IsNullOrEmpty(req.TargetId))
        {
            return BadRequest("TargetId is required for private or group messages.");
        }

        // 1️⃣  Tạo channel async
        await using var ch = await _conn.CreateChannelAsync(cancellationToken: ct);

        // 2️⃣  Đảm bảo exchange tồn tại
        await ch.ExchangeDeclareAsync(exchange: TopicExchange,
                                      type: ExchangeType.Topic,
                                      durable: true,
                                      autoDelete: false,
                                      arguments: null,
                                      cancellationToken: ct);

        // 3️⃣  Routing key tuỳ loại tin
        var routingKey = req.MessageType switch
        {
            MESSAGE_TYPE.PRIVATE => $"chat.private.{req.TargetId}",
            MESSAGE_TYPE.GROUP => $"chat.group.{req.TargetId}",
            _ => "chat.public.all"
        };

        // 4️⃣  Đóng gói payload
        var msg = new ChatMessage
        {
            User = req.SenderId,
            Content = req.Content,
            Timestamp = DateTime.UtcNow,
            MessageType = req.MessageType,
            TargetId = req.TargetId,
            DisplayName = req.SenderId
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(msg));

        // 5️⃣  Publish async
        await ch.BasicPublishAsync(exchange: TopicExchange,
                                   routingKey: routingKey,
                                   body: body,
                                   cancellationToken: ct);

        return Ok(new { Status = "Message routed successfully." });
    }
}
