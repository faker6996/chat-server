// File: SignalR/SignalRChatClientNotifier.cs
using ChatServer.Applications;
using ChatServer.Models;
using ChatServer.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ChatServer.SignalR;

public class SignalRChatClientNotifier : IChatClientNotifier
{
    private readonly IHubContext<ChatHub> _hubContext;

    public SignalRChatClientNotifier(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
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
}