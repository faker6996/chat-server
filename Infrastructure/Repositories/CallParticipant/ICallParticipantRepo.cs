using ChatServer.Core.Models;
using ChatServer.Infrastructure.Repositories.Base;

namespace ChatServer.Infrastructure.Repositories.CallParticipant;

public interface ICallParticipantRepo : IBaseRepository<Core.Models.CallParticipant>
{
    Task<List<Core.Models.CallParticipant>> GetActiveParticipantsAsync(string callId);
    
    Task<List<Core.Models.CallParticipant>> GetAllParticipantsAsync(string callId);
    
    Task<Core.Models.CallParticipant?> GetParticipantAsync(string callId, int userId);
    
    Task<bool> IsUserInCallAsync(string callId, int userId);
    
    Task<bool> LeaveCallAsync(string callId, int userId);
    
    Task<bool> ToggleMediaAsync(string callId, int userId, string mediaType, bool enabled);
    
    Task<bool> UpdateConnectionQualityAsync(string callId, int userId, string quality);
    
    Task<int> GetActiveParticipantsCountAsync(string callId);
}