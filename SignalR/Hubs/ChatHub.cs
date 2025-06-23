using Microsoft.AspNetCore.Authorization; // Sẽ cần khi bạn thêm xác thực
using Microsoft.AspNetCore.SignalR;

namespace ChatServer.SignalR.Hubs
{
    [Authorize] // Khi có xác thực, hãy bật dòng này lên
    public class ChatHub : Hub
    {
        // Client sẽ gọi method này khi muốn tham gia một group chat
        public async Task JoinGroup(string groupName)
        {
            // Context.ConnectionId là ID của kết nối hiện tại
            // Context.UserIdentifier là ID của user (nếu đã cấu hình IUserIdProvider)
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("User {UserIdentifier} joined group {GroupName}", Context.UserIdentifier, groupName);

            // Có thể gửi một tin nhắn hệ thống vào nhóm
            // await Clients.Group(groupName).SendAsync("SystemMessage", $"{Context.UserIdentifier} has joined the group.");
        }

        // Client sẽ gọi method này khi rời khỏi một group chat
        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("User {UserIdentifier} left group {GroupName}", Context.UserIdentifier, groupName);
        }

        public override Task OnConnectedAsync()
        {
            // Logic khi user kết nối
            _logger.LogInformation("User connected with ID: {UserIdentifier}", Context.UserIdentifier);
            return base.OnConnectedAsync();
        }

        private readonly ILogger<ChatHub> _logger;
        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
        }
    }
}