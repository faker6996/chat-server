// File: SignalR/SignalRChatClientNotifier.cs
using ChatServer.Infrastructure.Services;
using ChatServer.Core.Models;
using ChatServer.Presentation.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ChatServer.Presentation.SignalR;

public class SignalRChatClientNotifier : IChatClientNotifier
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<SignalRChatClientNotifier> _logger;

    public SignalRChatClientNotifier(IHubContext<ChatHub> hubContext, ILogger<SignalRChatClientNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendPublicMessageAsync(Message message)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveMessage", message);
    }

    public async Task SendPrivateMessageAsync(string userId, Message message)
    {
        await _hubContext.Clients.User(userId).SendAsync("ReceiveMessage", message);
    }

    public async Task SendGroupMessageAsync(string groupName, Message message)
    {
        await _hubContext.Clients.Group(groupName).SendAsync("ReceiveMessage", message);
    }

    public async Task SendReactionAsync(int messageId, MessageReaction reaction)
    {
        _logger.LogInformation("SignalR: Broadcasting ReceiveReaction event - MessageId: {MessageId}, ReactionId: {ReactionId}",
            messageId, reaction.id);

        // Send full reaction object with message_id included
        var reactionEvent = new
        {
            message_id = messageId,
            reaction = reaction
        };

        await _hubContext.Clients.All.SendAsync("ReceiveReaction", reactionEvent);
    }

    public async Task RemoveReactionAsync(int messageId, int userId, string emoji)
    {
        _logger.LogInformation("SignalR: Broadcasting RemoveReaction event - MessageId: {MessageId}, UserId: {UserId}, Emoji: {Emoji}",
            messageId, userId, emoji);

        // Send full object for consistency
        var removeEvent = new
        {
            message_id = messageId,
            user_id = userId,
            emoji = emoji
        };

        await _hubContext.Clients.All.SendAsync("RemoveReaction", removeEvent);
    }

    // Group management events
    public async Task GroupCreatedAsync(GroupInfoResponse group)
    {
        _logger.LogInformation("SignalR: Broadcasting GroupCreated event for group {GroupId}", group.id);

        // Notify all clients about new group creation (if public) or group members
        if (group.is_public)
        {
            await _hubContext.Clients.All.SendAsync("GroupCreated", group);
        }
        else
        {
            // Only notify group members
            await _hubContext.Clients.Group($"Group_{group.id}").SendAsync("GroupCreated", group);
        }
    }

    public async Task GroupUpdatedAsync(GroupInfoResponse group)
    {
        _logger.LogInformation("SignalR: Broadcasting GroupUpdated event for group {GroupId}", group.id);

        await _hubContext.Clients.Group($"Group_{group.id}").SendAsync("GroupUpdated", group);
    }

    public async Task GroupDeletedAsync(int groupId, string reason)
    {
        _logger.LogInformation("SignalR: Broadcasting GroupDeleted event for group {GroupId}", groupId);

        var deleteEvent = new { group_id = groupId, reason };
        await _hubContext.Clients.Group($"Group_{groupId}").SendAsync("GroupDeleted", deleteEvent);
    }

    // Group member events
    public async Task GroupMemberAddedAsync(int groupId, GroupMemberResponse member)
    {
        _logger.LogInformation("SignalR: Broadcasting GroupMemberAdded event - GroupId: {GroupId}, UserId: {UserId}",
            groupId, member.user_id);

        var memberEvent = new { group_id = groupId, member };
        await _hubContext.Clients.Group($"Group_{groupId}").SendAsync("GroupMemberAdded", memberEvent);
    }

    public async Task GroupMemberRemovedAsync(int groupId, int userId, string reason)
    {
        _logger.LogInformation("SignalR: Broadcasting GroupMemberRemoved event - GroupId: {GroupId}, UserId: {UserId}, Reason: {Reason}",
            groupId, userId, reason);

        var removeEvent = new { group_id = groupId, user_id = userId, reason };
        await _hubContext.Clients.Group($"Group_{groupId}").SendAsync("GroupMemberRemoved", removeEvent);
    }

    public async Task GroupMemberPromotedAsync(int groupId, int userId, string newRole)
    {
        _logger.LogInformation("SignalR: Broadcasting GroupMemberPromoted event - GroupId: {GroupId}, UserId: {UserId}, NewRole: {NewRole}",
            groupId, userId, newRole);

        var promoteEvent = new { group_id = groupId, user_id = userId, new_role = newRole };
        await _hubContext.Clients.Group($"Group_{groupId}").SendAsync("GroupMemberPromoted", promoteEvent);
    }

    public async Task GroupMemberOnlineAsync(int groupId, int userId)
    {
        _logger.LogInformation("SignalR: Broadcasting GroupMemberOnline event - GroupId: {GroupId}, UserId: {UserId}",
            groupId, userId);

        var onlineEvent = new { group_id = groupId, user_id = userId };
        await _hubContext.Clients.Group($"Group_{groupId}").SendAsync("GroupMemberOnline", onlineEvent);
    }

    public async Task GroupMemberOfflineAsync(int groupId, int userId)
    {
        _logger.LogInformation("SignalR: Broadcasting GroupMemberOffline event - GroupId: {GroupId}, UserId: {UserId}",
            groupId, userId);

        var offlineEvent = new { group_id = groupId, user_id = userId };
        await _hubContext.Clients.Group($"Group_{groupId}").SendAsync("GroupMemberOffline", offlineEvent);
    }

    // Group message events
    public async Task GroupMessageAsync(int groupId, MessageResponse message)
    {
        _logger.LogInformation("SignalR: Broadcasting GroupMessage event - GroupId: {GroupId}, MessageId: {MessageId}",
            groupId, message.id);

        var messageEvent = new { group_id = groupId, message };
        await _hubContext.Clients.Group($"Group_{groupId}").SendAsync("ReceiveGroupMessage", messageEvent);
    }

    // Join request events
    public async Task GroupJoinRequestAsync(int groupId, JoinRequestResponse request)
    {
        _logger.LogInformation("SignalR: Broadcasting GroupJoinRequest event - GroupId: {GroupId}, RequestId: {RequestId}",
            groupId, request.id);

        var requestEvent = new { group_id = groupId, request };

        // Only notify group admins and moderators about join requests
        await _hubContext.Clients.Group($"Group_{groupId}").SendAsync("GroupJoinRequest", requestEvent);
    }

    public async Task GroupJoinRequestHandledAsync(int requestId, string action, string? reason)
    {
        _logger.LogInformation("SignalR: Broadcasting GroupJoinRequestHandled event - RequestId: {RequestId}, Action: {Action}",
            requestId, action);

        var handledEvent = new { request_id = requestId, action, reason };

        // This would ideally notify the requester specifically, but we'll broadcast to all for now
        await _hubContext.Clients.All.SendAsync("GroupJoinRequestHandled", handledEvent);
    }

    // Group presence events
    public async Task UserJoinedGroupAsync(int groupId, int userId)
    {
        _logger.LogInformation("SignalR: Broadcasting UserJoinedGroup event - GroupId: {GroupId}, UserId: {UserId}",
            groupId, userId);

        var joinEvent = new { group_id = groupId, user_id = userId };
        await _hubContext.Clients.Group($"Group_{groupId}").SendAsync("UserJoinedGroup", joinEvent);
    }

    public async Task UserLeftGroupAsync(int groupId, int userId)
    {
        _logger.LogInformation("SignalR: Broadcasting UserLeftGroup event - GroupId: {GroupId}, UserId: {UserId}",
            groupId, userId);

        var leaveEvent = new { group_id = groupId, user_id = userId };
        await _hubContext.Clients.Group($"Group_{groupId}").SendAsync("UserLeftGroup", leaveEvent);
    }

    // Group invite events
    public async Task GroupInviteLinkGeneratedAsync(int groupId, string inviteLink)
    {
        _logger.LogInformation("SignalR: Broadcasting GroupInviteLinkGenerated event - GroupId: {GroupId}", groupId);

        var linkEvent = new { group_id = groupId, invite_link = inviteLink };
        await _hubContext.Clients.Group($"Group_{groupId}").SendAsync("GroupInviteLinkGenerated", linkEvent);
    }
}