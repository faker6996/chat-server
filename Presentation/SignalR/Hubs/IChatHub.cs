using ChatServer.Core.Models;
using System.Threading.Tasks;

namespace ChatServer.Presentation.SignalR.Hubs
{
    /// <summary>
    /// Interface định nghĩa các phương thức mà SERVER có thể gọi trên CLIENT.
    /// </summary>
    public interface IChatHub
    {
        // --- Phương thức cho Chat ---
        Task MessageFailed(string errorMessage);

        // --- Phương thức cho Video Call ---
        Task ReceiveCallOffer(string callerId, string offer);
        Task ReceiveCallAnswer(string calleeId, string answer);
        Task ReceiveIceCandidate(string senderId, string candidate);
        Task CallEnded(string endingUserId);

        // --- Phương thức cho Trạng thái Online ---
        Task UserOnline(string userId);
        Task UserOffline(string userId);

        // --- Phương thức cho Group Chat ---
        Task UserJoinedGroup(int groupId, int userId);
        Task UserLeftGroup(int groupId, int userId);
    }
}
