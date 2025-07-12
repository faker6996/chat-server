using ChatServer.Models;

namespace ChatServer.Repositories.Group;

public interface IGroupRepository
{
    // Group CRUD operations
    Task<Conversation> CreateGroupAsync(CreateGroupRequest request, int createdBy);
    Task<Conversation?> GetGroupByIdAsync(int groupId);
    Task<GroupInfoResponse?> GetGroupInfoAsync(int groupId, int currentUserId);
    Task<bool> UpdateGroupAsync(int groupId, UpdateGroupRequest request);
    Task<bool> DeleteGroupAsync(int groupId);
    Task<List<Conversation>> GetUserGroupsAsync(int userId);
    Task<List<GroupInfoResponse>> GetUserGroupsWithInfoAsync(int userId);

    // Member management
    Task<List<GroupMemberResponse>> GetGroupMembersAsync(int groupId);
    Task<GroupMemberResponse?> GetMemberAsync(int groupId, int userId);
    Task<bool> AddMemberAsync(int groupId, int userId, string role = "member");
    Task<bool> RemoveMemberAsync(int groupId, int userId);
    Task<bool> UpdateMemberRoleAsync(int groupId, int userId, string role);
    Task<bool> IsUserInGroupAsync(int groupId, int userId);
    Task<string?> GetUserRoleInGroupAsync(int groupId, int userId);
    Task<bool> UpdateUserOnlineStatusAsync(int groupId, int userId, bool isOnline);
    Task<int> GetGroupMemberCountAsync(int groupId);

    // Join requests
    Task<List<JoinRequestResponse>> GetPendingRequestsAsync(int groupId);
    Task<JoinRequestResponse?> GetJoinRequestAsync(int requestId);
    Task<int> CreateJoinRequestAsync(int groupId, int userId, string? message);
    Task<bool> HandleJoinRequestAsync(int requestId, string action, int reviewedBy, string? reason);
    Task<bool> HasPendingRequestAsync(int groupId, int userId);

    // Invite links
    Task<string> GetOrCreateInviteLinkAsync(int groupId);
    Task<int?> GetGroupByInviteLinkAsync(string inviteLink);
    Task<bool> RegenerateInviteLinkAsync(int groupId);

    // Permissions
    Task<bool> HasPermissionAsync(int groupId, int userId, string permission);
    Task<List<string>> GetUserPermissionsAsync(int groupId, int userId);
    Task<List<GroupPermission>> GetGroupPermissionsAsync(int groupId);
    Task<bool> UpdateGroupPermissionsAsync(int groupId, List<GroupPermission> permissions);

    // Statistics and utility methods
    Task<Dictionary<string, int>> GetGroupStatsAsync(int groupId);
    Task<List<int>> GetOnlineMembersAsync(int groupId);
    Task<bool> CanUserJoinGroupAsync(int groupId, int userId);
    Task<string> GenerateUniqueInviteCodeAsync();
}