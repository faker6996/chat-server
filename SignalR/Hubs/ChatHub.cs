using ChatServer.Applications;
using ChatServer.Models;
using ChatServer.Repositories.Messenger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace ChatServer.SignalR.Hubs
{
    // [Authorize]
    // Sửa lại để Hub sử dụng IChatHub cho client proxy
    public class ChatHub : Hub<IChatHub>
    {
        private readonly IMessageService _messageService;
        private readonly IUserRepo _userService; // Dùng IUserRepo như bạn đã cung cấp
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IMessageService messageService, IUserRepo userService, ILogger<ChatHub> logger)
        {
            _messageService = messageService;
            _userService = userService;
            _logger = logger;
        }

        // --- CÁC PHƯƠNG THỨC CLIENT GỌI SERVER ---

        public async Task SendMessage(SendMessageRequest req)
        {
            var result = await _messageService.SendMessageAsync(req);
            if (!result.IsSuccess)
            {
                await Clients.Caller.MessageFailed(result.ErrorMessage);
            }
        }

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

        public async Task SendCallOffer(string targetUserId, string offer)
        {
            var callerId = Context.UserIdentifier;
            await Clients.User(targetUserId).ReceiveCallOffer(callerId, offer);
        }

        public async Task SendCallAnswer(string targetUserId, string answer)
        {
            var calleeId = Context.UserIdentifier;
            await Clients.User(targetUserId).ReceiveCallAnswer(calleeId, answer);
        }

        public async Task SendIceCandidate(string targetUserId, string candidate)
        {
            var senderId = Context.UserIdentifier;
            await Clients.User(targetUserId).ReceiveIceCandidate(senderId, candidate);
        }

        public async Task EndCall(string targetUserId)
        {
            var endingUserId = Context.UserIdentifier;
            await Clients.User(targetUserId).CallEnded(endingUserId);
        }

        // --- CÁC PHƯƠNG THỨC LIFECYCLE CỦA HUB ---

        // Sửa lại: Gộp 2 phương thức OnConnectedAsync thành một
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("User connected with ID: {UserIdentifier}", Context.UserIdentifier);

            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                // Cập nhật trạng thái online trong DB
                await _userService.UpdateUserStatusAsync(int.Parse(userId), true);

                // Thông báo cho các client khác rằng user này đã online
                // Gửi cho tất cả client trừ người vừa kết nối
                await Clients.Others.UserOnline(userId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("User disconnected with ID: {UserIdentifier}", Context.UserIdentifier);

            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                // Cập nhật trạng thái offline và last_seen trong DB
                await _userService.UpdateUserStatusAsync(int.Parse(userId), false);

                // Thông báo cho các client khác rằng user này đã offline
                await Clients.Others.UserOffline(userId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
