using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using ChatServer.SignalR.Hubs;
using ChatServer.Models;

namespace ChatServer.Services
{
    public class RabbitMQConsumerService : BackgroundService
    {
        private readonly IConnection _rabbitConnection;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<RabbitMQConsumerService> _logger;
        private IChannel? _channel; // v7: IModel -> IChannel. Để là nullable (?) vì nó sẽ được khởi tạo bất đồng bộ.

        // Sử dụng lại các hằng số đã định nghĩa
        private const string TopicExchangeName = "chat_topic_exchange";
        private const string DurableQueueName = "chat_messages_queue";

        public RabbitMQConsumerService(IHubContext<ChatHub> hubContext, ILogger<RabbitMQConsumerService> logger, IConnection rabbitConnection)
        {
            _hubContext = hubContext;
            _logger = logger;
            _rabbitConnection = rabbitConnection;

            // v7: Không thể khởi tạo channel trong constructor nữa
            // vì phương thức CreateChannelAsync() là bất đồng bộ.
            // Việc khởi tạo sẽ được chuyển vào ExecuteAsync.
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RabbitMQ Consumer Service is starting.");

            try
            {
                // v7: Khởi tạo channel bất đồng bộ ở đây
                _channel = await _rabbitConnection.CreateChannelAsync(cancellationToken: stoppingToken);

                await _channel.ExchangeDeclareAsync(exchange: TopicExchangeName,
                                      type: ExchangeType.Topic,
                                      durable: true, // <-- THÊM DÒNG NÀY
                                      autoDelete: false, // <-- Thêm cả dòng này cho rõ ràng
                                      arguments: null,
                                      cancellationToken: stoppingToken);

                await _channel.QueueDeclareAsync(queue: DurableQueueName,
                                              durable: true,
                                              exclusive: false,
                                              autoDelete: false,
                                              arguments: null,
                                              cancellationToken: stoppingToken);

                await _channel.QueueBindAsync(queue: DurableQueueName,
                                          exchange: TopicExchangeName,
                                          routingKey: "chat.#", // Nhận tất cả các tin nhắn từ topic "chat"
                                          cancellationToken: stoppingToken);

                _logger.LogInformation("Waiting for messages from queue: {QueueName}", DurableQueueName);

                // v7: Dùng AsyncEventingBasicConsumer thay cho EventingBasicConsumer
                var consumer = new AsyncEventingBasicConsumer(_channel);

                // v7: Đăng ký vào sự kiện ReceivedAsync
                consumer.ReceivedAsync += async (sender, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var messageJson = Encoding.UTF8.GetString(body);
                    try
                    {
                        var message = JsonSerializer.Deserialize<ChatMessage>(messageJson);
                        var routingKey = ea.RoutingKey;

                        _logger.LogInformation("Received message with routing key: {RoutingKey}", routingKey);

                        var keyParts = routingKey.Split('.');
                        if (keyParts.Length < 2)
                        {
                            // Vẫn nên ack ngay cả khi tin nhắn không hợp lệ để tránh vòng lặp vô hạn
                            await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                            return;
                        }

                        var messageType = keyParts[1];

                        // Logic gửi tin nhắn qua SignalR không thay đổi,
                        // chỉ cần truyền stoppingToken vào cho đúng chuẩn
                        switch (messageType)
                        {
                            case "private" when keyParts.Length > 2:
                                var targetUserId = keyParts[2];
                                await _hubContext.Clients.User(targetUserId).SendAsync("ReceiveMessage", message, stoppingToken);
                                break;

                            case "group" when keyParts.Length > 2:
                                var targetGroupId = keyParts[2];
                                await _hubContext.Clients.Group(targetGroupId).SendAsync("ReceiveMessage", message, stoppingToken);
                                break;

                            case "public":
                                await _hubContext.Clients.All.SendAsync("ReceiveMessage", message, stoppingToken);
                                break;
                        }
                        await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process message: {Message}", messageJson);
                    }
                };

                // v7: Bắt đầu lắng nghe bất đồng bộ
                await _channel.BasicConsumeAsync(queue: DurableQueueName,
                                             autoAck: false,
                                             consumer: consumer,
                                             cancellationToken: stoppingToken);

                // Giữ cho background service chạy
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
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