using ChatServer.Models;
using System.Threading.Tasks;

namespace ChatServer.SignalR.Hubs
{
    public interface IHubClient
    {
        // Phương thức cho chat
        Task ReceiveMessage(Message message);
        Task MessageFailed(string errorMessage);

        // Phương thức cho video call
        Task ReceiveCallOffer(string callingUserId, string offer);
        Task ReceiveCallAnswer(string answeringUserId, string answer);
        Task ReceiveIceCandidate(string sendingUserId, string candidate);
        Task CallEnded(string disconnectingUserId);
    }
}