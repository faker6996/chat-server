// File: Repositories/MessagePublisher.cs
using ChatServer.Infrastructure.Services; // using interface từ lớp Application
using ChatServer.Core.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

// Namespace có thể là ChatServer.Repositories
namespace ChatServer.Infrastructure.Repositories;

public class MessagePublisher : IMessagePublisher
{
    private readonly IConnection _rabbitMqConnection;
    private readonly ILogger<MessagePublisher> _logger;

    // Tiêm IConnection của RabbitMQ và ILogger vào đây
    public MessagePublisher(IConnection rabbitMqConnection, ILogger<MessagePublisher> logger)
    {
        _rabbitMqConnection = rabbitMqConnection;
        _logger = logger;
    }

    // Triển khai phương thức từ interface IMessagePublisher
    public async Task PublishAsync(string exchange, string routingKey, Message message)
    {
        try
        {
            await using var channel = await _rabbitMqConnection.CreateChannelAsync();

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            await channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
                body: body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ publish failed for routing key {routingKey}", routingKey);
            throw;
        }
    }
}