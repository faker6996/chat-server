// File: Application/IChatClientNotifier.cs
using ChatServer.Models;

namespace ChatServer.Applications;

// Interface định nghĩa khả năng thông báo cho client
public interface IChatClientNotifier
{
    Task SendPublicMessageAsync(Message message);
    Task SendPrivateMessageAsync(string userId, Message message);
    Task SendGroupMessageAsync(string groupName, Message message);
}