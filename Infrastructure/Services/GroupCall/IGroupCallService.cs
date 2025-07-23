using ChatServer.Core.Models;

namespace ChatServer.Infrastructure.Services.GroupCall;

public interface IGroupCallService
{
    Task<GroupCallServiceResult> StartGroupCallAsync(int groupId, int initiatorId, StartGroupCallRequest request);
    
    Task<GroupCallServiceResult> JoinGroupCallAsync(string callId, int userId, JoinGroupCallRequest request);
    
    Task<GroupCallServiceResult> LeaveGroupCallAsync(string callId, int userId);
    
    Task<GroupCallServiceResult> EndGroupCallAsync(string callId, int userId);
    
    Task<GroupCallServiceResult> GetActiveGroupCallAsync(int groupId);
    
    Task<GroupCallServiceResult> GetCallParticipantsAsync(string callId);
    
    Task<GroupCallServiceResult> ToggleMediaAsync(string callId, int userId, ToggleMediaRequest request);
    
    Task<GroupCallServiceResult> UpdateConnectionQualityAsync(string callId, int userId, string quality);
    
    Task<GroupCallServiceResult> GetGroupCallHistoryAsync(int groupId, int page = 1, int limit = 20);
    
    Task<bool> ValidateUserCanJoinCallAsync(string callId, int userId);
    
    Task<bool> IsUserGroupMemberAsync(int groupId, int userId);
}