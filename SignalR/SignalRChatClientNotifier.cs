// File: SignalR/SignalRChatClientNotifier.cs
using ChatServer.Applications;
using ChatServer.Models;
using ChatServer.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ChatServer.SignalR;

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
        var reactionEvent = new {
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
        var removeEvent = new {
            message_id = messageId,
            user_id = userId,
            emoji = emoji
        };
        
        await _hubContext.Clients.All.SendAsync("RemoveReaction", removeEvent);
    }
}