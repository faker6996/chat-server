using ChatServer.Infrastructure.Services;
using ChatServer.Infrastructure.Services.GroupCall;
using ChatServer.Core.Models;
using ChatServer.Core.Constants;
using ChatServer.Infrastructure.Repositories.Messenger;
using ChatServer.Infrastructure.Repositories.Group;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace ChatServer.Presentation.SignalR.Hubs
{
    [Authorize]
    public class ChatHub : Hub<IChatHub>
    {
        private readonly IMessageService _messageService;
        private readonly IUserRepo _userService; // Dùng IUserRepo như bạn đã cung cấp
        private readonly IGroupRepository _groupRepo;
        private readonly IGroupCallService _groupCallService;
        private readonly IChatClientNotifier _clientNotifier;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IMessageService messageService, IUserRepo userService, IGroupRepository groupRepo, 
            IGroupCallService groupCallService, IChatClientNotifier clientNotifier, ILogger<ChatHub> logger)
        {
            _messageService = messageService;
            _userService = userService;
            _groupRepo = groupRepo;
            _groupCallService = groupCallService;
            _clientNotifier = clientNotifier;
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
                    message_type = MESSAGE_TYPE.GROUP,
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

        public async Task SendIceCandidate(string targetUserId, string candidate)
        {
            var senderId = Context.UserIdentifier ?? "Unknown";
            _logger.LogInformation($"🧊 ICE candidate from user {senderId} to {targetUserId}");
            await Clients.User(targetUserId).ReceiveIceCandidate(senderId, candidate);
        }

        public async Task SendCallOffer(string targetUserId, string offer, string callType = "video")
        {
            try
            {
                var callerId = Context.UserIdentifier ?? "Unknown";
                
                // Log for debugging
                _logger.LogInformation($"📞 SendCallOffer: from {callerId} to {targetUserId}, type: {callType}");

                // Validate inputs
                if (string.IsNullOrEmpty(targetUserId))
                {
                    throw new ArgumentException("Target user ID cannot be empty");
                }

                if (string.IsNullOrEmpty(offer))
                {
                    throw new ArgumentException("Offer cannot be empty");
                }

                // Validate and normalize callType
                callType = callType?.ToLower() ?? "video";
                if (callType != "video" && callType != "audio")
                {
                    callType = "video"; // default to video call
                }

                // Send to target user
                await Clients.User(targetUserId).ReceiveCallOffer(callerId, offer, callType);

                _logger.LogInformation($"📞 Call offer sent successfully to {targetUserId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error in SendCallOffer: {ex.Message}");
                throw; // Re-throw to send error back to client
            }
        }

        public async Task SendCallAnswer(string targetUserId, string answer)
        {
            var calleeId = Context.UserIdentifier ?? "Unknown";
            _logger.LogInformation($"📞 Call_answer offer from {calleeId} to {targetUserId}");
            await Clients.User(targetUserId).ReceiveCallAnswer(calleeId, answer);
        }

        public async Task EndCall(string targetUserId)
        {
            var endingUserId = Context.UserIdentifier ?? "Unknown";
            await Clients.User(targetUserId).CallEnded(endingUserId);
        }

        // === GROUP CALL METHODS ===

        public async Task StartGroupCall(string groupId, string callType)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                await Clients.Caller.MessageFailed("User not authenticated");
                return;
            }

            if (!int.TryParse(groupId, out int groupIdInt))
            {
                await Clients.Caller.MessageFailed("Invalid group ID");
                return;
            }

            try
            {
                var request = new StartGroupCallRequest
                {
                    call_type = callType,
                    max_participants = 10
                };

                var result = await _groupCallService.StartGroupCallAsync(groupIdInt, userId.Value, request);

                if (!result.IsSuccess)
                {
                    await Clients.Caller.MessageFailed(result.ErrorMessage ?? "Failed to start group call");
                    return;
                }

                var callResponse = result.Data as GroupCallResponse;
                if (callResponse != null)
                {
                    // Join the call-specific SignalR group
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Call_{callResponse.id}");

                    // Debug: Check group members
                    var groupMembers = await _groupRepo.GetGroupMembersAsync(groupIdInt);
                    _logger.LogInformation("DEBUG: Group {GroupId} has {MemberCount} members. Broadcasting GroupCallStarted to Group_{GroupId}",
                        groupIdInt, groupMembers.Count, groupIdInt);

                    // Notify all group members about the new call
                    await _clientNotifier.GroupCallStartedAsync(groupIdInt, callResponse);

                    _logger.LogInformation("Group call {CallId} started by user {UserId} in group {GroupId}. Notification sent to Group_{GroupId}",
                        callResponse.id, userId.Value, groupIdInt, groupIdInt);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting group call in group {GroupId} by user {UserId}", groupIdInt, userId.Value);
                await Clients.Caller.MessageFailed("Failed to start group call");
            }
        }

        public async Task JoinGroupCall(string callId)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                await Clients.Caller.MessageFailed("User not authenticated");
                return;
            }

            try
            {
                var request = new JoinGroupCallRequest
                {
                    is_audio_enabled = true,
                    is_video_enabled = true
                };

                var result = await _groupCallService.JoinGroupCallAsync(callId, userId.Value, request);

                if (!result.IsSuccess)
                {
                    await Clients.Caller.MessageFailed(result.ErrorMessage ?? "Failed to join group call");
                    return;
                }

                // Join the call-specific SignalR group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Call_{callId}");

                var callResponse = result.Data as GroupCallResponse;
                if (callResponse != null)
                {
                    var participant = callResponse.participants.FirstOrDefault(p => p.user_id == userId.Value);
                    if (participant != null)
                    {
                        // Notify group members about new participant
                        await _clientNotifier.GroupCallParticipantJoinedAsync(callResponse.group_id, callId, participant);
                    }
                }

                _logger.LogInformation("User {UserId} joined group call {CallId}", userId.Value, callId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining group call {CallId} for user {UserId}", callId, userId.Value);
                await Clients.Caller.MessageFailed("Failed to join group call");
            }
        }

        public async Task LeaveGroupCall(string callId)
        {
            var userId = GetUserId();
            if (userId == null) return;

            try
            {
                // Get call info before leaving
                var activeCallResult = await _groupCallService.GetActiveGroupCallAsync(0); // We'll need the groupId
                GroupCallResponse? callInfo = null;
                
                // Leave the call
                var result = await _groupCallService.LeaveGroupCallAsync(callId, userId.Value);

                if (!result.IsSuccess)
                {
                    await Clients.Caller.MessageFailed(result.ErrorMessage ?? "Failed to leave group call");
                    return;
                }

                // Leave the call-specific SignalR group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Call_{callId}");

                // Notify group members about participant leaving
                if (callInfo != null)
                {
                    await _clientNotifier.GroupCallParticipantLeftAsync(callInfo.group_id, callId, userId.Value, "user_left");
                }

                _logger.LogInformation("User {UserId} left group call {CallId}", userId.Value, callId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving group call {CallId} for user {UserId}", callId, userId.Value);
                await Clients.Caller.MessageFailed("Failed to leave group call");
            }
        }

        public async Task EndGroupCall(string callId)
        {
            var userId = GetUserId();
            if (userId == null)
            {
                await Clients.Caller.MessageFailed("User not authenticated");
                return;
            }

            try
            {
                var result = await _groupCallService.EndGroupCallAsync(callId, userId.Value);

                if (!result.IsSuccess)
                {
                    await Clients.Caller.MessageFailed(result.ErrorMessage ?? "Failed to end group call");
                    return;
                }

                // Notify all call participants that call ended
                await Clients.Group($"Call_{callId}").MessageFailed($"Group call {callId} ended");

                _logger.LogInformation("Group call {CallId} ended by user {UserId}", callId, userId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending group call {CallId} by user {UserId}", callId, userId.Value);
                await Clients.Caller.MessageFailed("Failed to end group call");
            }
        }

        public async Task ToggleGroupCallMedia(string callId, string mediaType, bool enabled)
        {
            var userId = GetUserId();
            if (userId == null) return;

            try
            {
                var request = new ToggleMediaRequest
                {
                    media_type = mediaType,
                    enabled = enabled
                };

                var result = await _groupCallService.ToggleMediaAsync(callId, userId.Value, request);

                if (!result.IsSuccess)
                {
                    await Clients.Caller.MessageFailed(result.ErrorMessage ?? "Failed to toggle media");
                    return;
                }

                // Notify call participants about media toggle - using MessageFailed as placeholder
                await Clients.GroupExcept($"Call_{callId}", Context.ConnectionId)
                    .MessageFailed($"User {userId.Value} toggled {mediaType} to {enabled}");

                _logger.LogInformation("User {UserId} toggled {MediaType} to {Enabled} in call {CallId}",
                    userId.Value, mediaType, enabled, callId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling media for user {UserId} in call {CallId}", userId.Value, callId);
                await Clients.Caller.MessageFailed("Failed to toggle media");
            }
        }

        // WebRTC signaling methods for group calls
        public async Task SendGroupCallOffer(string callId, string targetUserId, string offerData)
        {
            var senderId = Context.UserIdentifier ?? "Unknown";
            _logger.LogInformation("Group call offer from user {SenderId} to {TargetUserId} in call {CallId}", 
                senderId, targetUserId, callId);

            await Clients.User(targetUserId).MessageFailed($"Group call offer from {senderId}");
        }

        public async Task SendGroupCallAnswer(string callId, string targetUserId, string answerData)
        {
            var senderId = Context.UserIdentifier ?? "Unknown";
            _logger.LogInformation("Group call answer from user {SenderId} to {TargetUserId} in call {CallId}", 
                senderId, targetUserId, callId);

            await Clients.User(targetUserId).MessageFailed($"Group call answer from {senderId}");
        }

        public async Task SendGroupIceCandidate(string callId, string targetUserId, string candidateData)
        {
            var senderId = Context.UserIdentifier ?? "Unknown";
            _logger.LogInformation("Group ICE candidate from user {SenderId} to {TargetUserId} in call {CallId}", 
                senderId, targetUserId, callId);

            await Clients.User(targetUserId).MessageFailed($"Group ICE candidate from {senderId}");
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
                _logger.LogInformation("DEBUG: User {UserId} connecting to {GroupCount} groups", userId.Value, userGroups.Count);
                
                foreach (var group in userGroups)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Group_{group.id}");
                    await _groupRepo.UpdateUserOnlineStatusAsync(group.id, userId.Value, true);

                    _logger.LogInformation("DEBUG: User {UserId} joined SignalR group 'Group_{GroupId}'", userId.Value, group.id);

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
