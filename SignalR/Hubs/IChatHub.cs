using ChatServer.Models;
using System.Threading.Tasks;

namespace ChatServer.SignalR.Hubs
{
    public interface IChatHub
    {
        // Phương thức cho chat
        Task SendMessage(SendMessageRequest req);
        Task JoinGroup(string groupName);
        Task LeaveGroup(string groupName);

        // Phương thức cho video call
        Task SendCallOffer(string targetUserId, string offer);
        Task SendCallAnswer(string targetUserId, string answer);
        Task SendIceCandidate(string targetUserId, string candidate);
        Task EndCall(string targetUserId);
    }
}