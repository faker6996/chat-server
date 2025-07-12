using ChatServer.Applications;
using ChatServer.Models;
using ChatServer.Repositories.Messenger;
using ChatServer.Repositories.Group;
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
        private readonly IGroupRepository _groupRepo;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IMessageService messageService, IUserRepo userService, IGroupRepository groupRepo, ILogger<ChatHub> logger)
        {
            _messageService = messageService;
            _userService = userService;
            _groupRepo = groupRepo;
            _logger = logger;
        }

        // --- CÁC PHƯƠNG THỨC CLIENT GỌI SERVER ---

        public async Task SendMessage(SendMessageRequest req)
        {
            var result = await _messageService.SendMessageAsync(req);
            if (!result.IsSuccess)
            {
                await Clients.Caller.MessageFailed(result.ErrorMessage ?? "Unknown error occurred");
            }
        }

        // Group management methods
        public async Task JoinGroup(string groupId)
        {
            var userId = GetUserId();
            if (userId == null) return;

            if (int.TryParse(groupId, out int groupIdInt))
            {
                // Verify user is actually a member of this group
                if (await _groupRepo.IsUserInGroupAsync(groupIdInt, userId.Value))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Group_{groupId}");
                    
                    // Update user online status for this group
                    await _groupRepo.UpdateUserOnlineStatusAsync(groupIdInt, userId.Value, true);
                    
                    _logger.LogInformation("User {UserId} joined SignalR group {GroupId}", userId.Value, groupId);
                    
                    // Notify other group members
                    await Clients.GroupExcept($"Group_{groupId}", Context.ConnectionId)
                        .UserJoinedGroup(groupIdInt, userId.Value);
                }
                else
                {
                    await Clients.Caller.MessageFailed("You are not a member of this group");
                }
            }
        }

        public async Task LeaveGroup(string groupId)
        {
            var userId = GetUserId();
            if (userId == null) return;

            if (int.TryParse(groupId, out int groupIdInt))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Group_{groupId}");
                
                // Update user online status
                await _groupRepo.UpdateUserOnlineStatusAsync(groupIdInt, userId.Value, false);
                
                _logger.LogInformation("User {UserId} left SignalR group {GroupId}", userId.Value, groupId);
                
                // Notify other group members
                await Clients.Group($"Group_{groupId}")
                    .UserLeftGroup(groupIdInt, userId.Value);
            }
        }

        public async Task SendGroupMessage(SendGroupMessageRequest request)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                await Clients.Caller.MessageFailed("User not authenticated");
                return;
            }

            try
            {
                // Validate user is member of the group
                if (!await _groupRepo.IsUserInGroupAsync(request.group_id, userId.Value))
                {
                    await Clients.Caller.MessageFailed("You are not a member of this group");
                    return;
                }

                // Create message via existing message service
                var messageRequest = new SendMessageRequest
                {
                    sender_id = userId.Value,
                    conversation_id = request.group_id,
                    content = request.content,
                    message_type = Constants.MESSAGE_TYPE.GROUP,
                    content_type = request.content_type,
                    target_id = request.group_id,
                    reply_to_message_id = request.reply_to_message_id,
                    attachments = request.attachments
                };

                var result = await _messageService.SendMessageAsync(messageRequest);
                
                if (!result.IsSuccess)
                {
                    await Clients.Caller.MessageFailed(result.ErrorMessage ?? "Failed to send group message");
                    return;
                }

                _logger.LogInformation("Group message sent by user {UserId} to group {GroupId}", 
                    userId.Value, request.group_id);

                // Message will be broadcast via RabbitMQ consumer
                // No need to broadcast here as it's handled by the consumer service
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending group message from user {UserId} to group {GroupId}", 
                    userId.Value, request.group_id);
                await Clients.Caller.MessageFailed("Failed to send message");
            }
        }

        public async Task SendCallOffer(string targetUserId, string offer)
        {
            var callerId = Context.UserIdentifier ?? "Unknown";
            await Clients.User(targetUserId).ReceiveCallOffer(callerId, offer);
        }

        public async Task SendCallAnswer(string targetUserId, string answer)
        {
            var calleeId = Context.UserIdentifier ?? "Unknown";
            await Clients.User(targetUserId).ReceiveCallAnswer(calleeId, answer);
        }

        public async Task SendIceCandidate(string targetUserId, string candidate)
        {
            var senderId = Context.UserIdentifier ?? "Unknown";
            await Clients.User(targetUserId).ReceiveIceCandidate(senderId, candidate);
        }

        public async Task EndCall(string targetUserId)
        {
            var endingUserId = Context.UserIdentifier ?? "Unknown";
            await Clients.User(targetUserId).CallEnded(endingUserId);
        }

        // --- HELPER METHODS ---

        private int? GetUserId()
        {
            var userIdStr = Context.UserIdentifier;
            if (int.TryParse(userIdStr, out int userId))
            {
                return userId;
            }
            return null;
        }

        // --- CÁC PHƯƠNG THỨC LIFECYCLE CỦA HUB ---

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("User connected with ID: {UserIdentifier}", Context.UserIdentifier);

            var userId = GetUserId();
            if (userId.HasValue)
            {
                // Cập nhật trạng thái online trong DB
                await _userService.UpdateUserStatusAsync(userId.Value, true);

                // Join all user's groups automatically
                var userGroups = await _groupRepo.GetUserGroupsAsync(userId.Value);
                foreach (var group in userGroups)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Group_{group.id}");
                    await _groupRepo.UpdateUserOnlineStatusAsync(group.id, userId.Value, true);
                    
                    // Notify group members that user is online
                    await Clients.GroupExcept($"Group_{group.id}", Context.ConnectionId)
                        .UserJoinedGroup(group.id, userId.Value);
                }

                // Thông báo cho các client khác rằng user này đã online
                await Clients.Others.UserOnline(userId.Value.ToString());

                _logger.LogInformation("User {UserId} connected and joined {GroupCount} groups", 
                    userId.Value, userGroups.Count);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("User disconnected with ID: {UserIdentifier}", Context.UserIdentifier);

            var userId = GetUserId();
            if (userId.HasValue)
            {
                // Cập nhật trạng thái offline và last_seen trong DB
                await _userService.UpdateUserStatusAsync(userId.Value, false);

                // Update offline status for all groups
                var userGroups = await _groupRepo.GetUserGroupsAsync(userId.Value);
                foreach (var group in userGroups)
                {
                    await _groupRepo.UpdateUserOnlineStatusAsync(group.id, userId.Value, false);
                    
                    // Notify group members
                    await Clients.Group($"Group_{group.id}")
                        .UserLeftGroup(group.id, userId.Value);
                }

                // Thông báo cho các client khác rằng user này đã offline
                await Clients.Others.UserOffline(userId.Value.ToString());

                _logger.LogInformation("User {UserId} disconnected from {GroupCount} groups", 
                    userId.Value, userGroups.Count);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
