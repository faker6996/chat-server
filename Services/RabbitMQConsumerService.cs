using ChatServer.Applications; // <-- SỬ DỤNG
using ChatServer.Models;
using ChatServer.Repositories.Messenger; // <-- SỬ DỤNG
using Microsoft.Extensions.DependencyInjection; // <-- SỬ DỤNG cho IServiceScopeFactory
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace ChatServer.Services
{
    /// <summary>
    /// Service chạy nền để lắng nghe và xử lý tin nhắn từ RabbitMQ.
    /// Nó hoạt động như một "entry point" phía sau, điều phối việc lưu trữ và thông báo.
    /// </summary>
    public class RabbitMQConsumerService : BackgroundService
    {
        private readonly ILogger<RabbitMQConsumerService> _logger;
        private readonly IConnection _rabbitConnection;
        private readonly IServiceScopeFactory _scopeFactory; // Dùng để tạo scope cho các service scoped như Repo và Notifier
        private IChannel? _channel;

        private const string TopicExchangeName = "chat_topic_exchange";
        private const string DurableQueueName = "chat_messages_queue";

        /// <summary>
        /// Constructor mới: Gỡ bỏ IHubContext, thêm IServiceScopeFactory.
        /// </summary>
        public RabbitMQConsumerService(
            ILogger<RabbitMQConsumerService> logger,
            IConnection rabbitConnection,
            IServiceScopeFactory scopeFactory) // <-- Tiêm IServiceScopeFactory
        {
            _logger = logger;
            _rabbitConnection = rabbitConnection;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RabbitMQ Consumer Service is starting.");

            try
            {
                // Phần thiết lập kết nối, channel, exchange và queue không thay đổi
                _channel = await _rabbitConnection.CreateChannelAsync(cancellationToken: stoppingToken);

                await _channel.ExchangeDeclareAsync(exchange: TopicExchangeName, type: ExchangeType.Topic, durable: true, autoDelete: false, arguments: null, cancellationToken: stoppingToken);
                await _channel.QueueDeclareAsync(queue: DurableQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);
                await _channel.QueueBindAsync(queue: DurableQueueName, exchange: TopicExchangeName, routingKey: "chat.#", cancellationToken: stoppingToken);

                _logger.LogInformation("Waiting for messages from queue: {QueueName}", DurableQueueName);

                var consumer = new AsyncEventingBasicConsumer(_channel);

                // --- ĐÂY LÀ PHẦN LOGIC CỐT LÕI ĐÃ ĐƯỢC THAY ĐỔI ---
                consumer.ReceivedAsync += async (sender, ea) =>
                {
                    // Tạo ra một scope mới cho mỗi lần xử lý tin nhắn.
                    // Điều này rất quan trọng vì IMessageRepo và IChatClientNotifier là Scoped service.
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var messageRepo = scope.ServiceProvider.GetRequiredService<IMessageRepo>();
                    var clientNotifier = scope.ServiceProvider.GetRequiredService<IChatClientNotifier>();

                    var body = ea.Body.ToArray();
                    var messageJson = Encoding.UTF8.GetString(body);

                    try
                    {
                        var message = JsonSerializer.Deserialize<Message>(messageJson);
                        if (message == null)
                        {
                            _logger.LogWarning("Received a message that could not be deserialized.");
                            // Vẫn ACK để loại bỏ tin nhắn không hợp lệ khỏi queue
                            await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                            return;
                        }

                        // === BƯỚC XỬ LÝ THEO "CÁCH 2" ===

                        // 1. LƯU TIN NHẮN VÀO DATABASE
                        await messageRepo.InsertAsync(message);
                        _logger.LogInformation("Message with sender_id {SenderId} saved to database.", message.sender_id);

                        // 2. GỬI THÔNG BÁO REAL-TIME TỚI CLIENT QUA NOTIFIER
                        var routingKey = ea.RoutingKey;
                        _logger.LogInformation("Notifying clients for message with routing key: {RoutingKey}", routingKey);

                        var keyParts = routingKey.Split('.');
                        var messageType = keyParts.Length > 1 ? keyParts[1] : null;

                        switch (messageType)
                        {
                            case "private" when keyParts.Length > 2:
                                await clientNotifier.SendPrivateMessageAsync(keyParts[2], message);
                                break;
                            case "group" when keyParts.Length > 2:
                                await clientNotifier.SendGroupMessageAsync(keyParts[2], message);
                                break;
                            case "public":
                                await clientNotifier.SendPublicMessageAsync(message);
                                break;
                            default:
                                _logger.LogWarning("Unknown routing key format: {RoutingKey}", routingKey);
                                break;
                        }

                        // Chỉ xác nhận (ACK) sau khi đã xử lý thành công (lưu DB và gửi thông báo)
                        await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "Failed to deserialize message: {Message}", messageJson);
                        await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken); // Loại bỏ tin nhắn hỏng
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An unexpected error occurred while processing message: {Message}", messageJson);
                        // Khi có lỗi, chúng ta không ACK (BasicAck) tin nhắn.
                        // RabbitMQ sẽ giữ lại và thử gửi lại cho một consumer khác (hoặc chính consumer này sau một thời gian).
                        // Cân nhắc dùng BasicNack và cấu hình Dead Letter Exchange để xử lý các tin nhắn lỗi vĩnh viễn.
                    }
                };

                await _channel.BasicConsumeAsync(queue: DurableQueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "An error occurred in RabbitMQ Consumer Service.");
            }
            finally
            {
                _logger.LogInformation("RabbitMQ Consumer Service is stopping.");
            }
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            base.Dispose();
        }
    }
}