// File: Application/IMessagePublisher.cs
using ChatServer.Core.Models;

namespace ChatServer.Infrastructure.Services;

public interface IMessagePublisher
{
    /// <summary>
    /// Publish một tin nhắn tới một exchange với một routing key cụ thể.
    /// </summary>
    Task PublishAsync(string exchange, string routingKey, Message message);
}