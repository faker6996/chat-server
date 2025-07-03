using ChatServer.Applications;
using ChatServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks; // Đảm bảo có using này

namespace ChatServer.SignalR.Hubs
{
    // Cập nhật class để kế thừa từ Hub<IHubClient> và triển khai IChatHub
    // [Authorize]
    public class ChatHub : Hub<IHubClient>, IChatHub
    {
        private readonly IMessageService _messageService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IMessageService messageService, ILogger<ChatHub> logger)
        {
            _messageService = messageService;
            _logger = logger;
        }

        // --- CÁC PHƯƠNG THỨC CHAT ---

        public async Task SendMessage(SendMessageRequest req)
        {
            var result = await _messageService.SendMessageAsync(req);

            if (!result.IsSuccess)
            {
                // Gọi phương thức MessageFailed đã được định nghĩa trong IHubClient
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

        // --- CÁC PHƯƠNG THỨC VIDEO CALL ---

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
            // THÊM CÁC DÒNG LOG NÀY ĐỂ DEBUG
            _logger.LogInformation("--- Received ICE Candidate ---");
            _logger.LogInformation("Sender ID (from Context): {UserIdentifier}", Context.UserIdentifier);
            _logger.LogInformation("Target User ID (from client): {TargetUserId}", targetUserId);

            var senderId = Context.UserIdentifier;
            await Clients.User(targetUserId).ReceiveIceCandidate(senderId, candidate);
        }

        public async Task EndCall(string targetUserId)
        {
            var endingUserId = Context.UserIdentifier;
            await Clients.User(targetUserId).CallEnded(endingUserId);
        }

        // --- CÁC PHƯƠNG THỨC LIFECYCLE ---

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation("User connected with ID: {UserIdentifier}", Context.UserIdentifier);
            return base.OnConnectedAsync();
        }
    }
}