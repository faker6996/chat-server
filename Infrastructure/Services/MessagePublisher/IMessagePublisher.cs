// File: Application/IMessagePublisher.cs
using ChatServer.Models;

namespace ChatServer.Applications;

public interface IMessagePublisher
{
    /// <summary>
    /// Publish một tin nhắn tới một exchange với một routing key cụ thể.
    /// </summary>
    Task PublishAsync(string exchange, string routingKey, Message message);
}