using ChatServer.Core.Models;
using ChatServer.Infrastructure.Repositories.Base;

namespace ChatServer.Infrastructure.Repositories.GroupCall;

public interface IGroupCallRepo : IBaseRepository<Core.Models.GroupCall>
{
    Task<Core.Models.GroupCall?> GetByStringIdAsync(string id);
    
    Task<string> InsertGroupCallAsync(Core.Models.GroupCall groupCall);
    
    Task<Core.Models.GroupCall?> GetActiveGroupCallAsync(int groupId);
    
    Task<List<Core.Models.GroupCall>> GetGroupCallHistoryAsync(int groupId, int offset, int limit);
    
    Task<bool> UpdateGroupCallStatusAsync(string callId, string status, DateTime? endedAt = null);
    
    Task<bool> HasActiveCallAsync(int groupId);
}