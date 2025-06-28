using ChatServer.Applications; // <-- SỬ DỤNG LỚP APPLICATION
using ChatServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatServer.SignalR.Hubs
{
    // [Authorize] // Bật lại khi có xác thực
    public class ChatHub : Hub
    {
        private readonly IMessageService _messageService;
        private readonly ILogger<ChatHub> _logger;

        // Tiêm cả IMessageService và ILogger
        public ChatHub(IMessageService messageService, ILogger<ChatHub> logger)
        {
            _messageService = messageService;
            _logger = logger;
        }

        // THÊM PHƯƠNG THỨC NÀY: Điểm vào cho client gửi tin nhắn
        // Tên phương thức này ("SendMessage") phải khớp với tên mà client sẽ gọi
        public async Task SendMessage(SendMessageRequest req)
        {
            // Hub chỉ gọi đến Service, không tự xử lý logic
            var result = await _messageService.SendMessageAsync(req);

            if (!result.IsSuccess)
            {
                // Nếu có lỗi (ví dụ: validation), gửi phản hồi lỗi về cho chính người gửi đó
                await Clients.Caller.SendAsync("MessageFailed", result.ErrorMessage);
            }
            // Hub không cần làm gì thêm. Việc gửi tin tới người nhận sẽ do RabbitMQConsumerService đảm nhiệm.
        }


        // --- CÁC PHƯƠNG THỨC HIỆN TẠI VẪN GIỮ NGUYÊN ---
        // Chúng là logic thuộc về tầng giao diện của SignalR, không phải business logic.

        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("User {UserIdentifier} joined group {GroupName}", Context.UserIdentifier, groupName);
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("User {UserIdentifier} left group {GroupName}", Context.UserIdentifier, groupName);
        }

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation("User connected with ID: {UserIdentifier}", Context.UserIdentifier);
            return base.OnConnectedAsync();
        }
    }
}