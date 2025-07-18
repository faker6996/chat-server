using ChatServer.Core.Models;
using ChatServer.Infrastructure.Repositories.Group;
using ChatServer.Infrastructure.Repositories.Base;
using Dapper;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace ChatServer.Infrastructure.Repositories.Group;

// Simple concrete implementation for ConversationParticipant
public class ConversationParticipantRepository : BaseRepository<ConversationParticipant>
{
    public ConversationParticipantRepository(IDbConnection dbConnection) : base(dbConnection) { }
}

// Simple concrete implementation for GroupJoinRequest
public class GroupJoinRequestRepository : BaseRepository<GroupJoinRequest>
{
    public GroupJoinRequestRepository(IDbConnection dbConnection) : base(dbConnection) { }
}

public class GroupRepository : BaseRepository<Conversation>, IGroupRepository
{
    private readonly ConversationParticipantRepository _participantRepo;
    private readonly GroupJoinRequestRepository _joinRequestRepo;

    public GroupRepository(IDbConnection connection) : base(connection)
    {
        _participantRepo = new ConversationParticipantRepository(connection);
        _joinRequestRepo = new GroupJoinRequestRepository(connection);
    }

    // Group CRUD operations
    public async Task<Conversation> CreateGroupAsync(CreateGroupRequest request, int createdBy)
    {
        var conversation = new Conversation
        {
            is_group = true,
            name = request.name,
            description = request.description,
            avatar_url = request.avatar_url,
            max_members = request.max_members,
            is_public = request.is_public,
            require_approval = request.require_approval,
            created_by = createdBy,
            created_at = DateTime.UtcNow
        };

        conversation.id = await InsertAsync(conversation);
        return conversation;
    }

    public async Task<Conversation?> GetGroupByIdAsync(int groupId)
    {
        var sql = @"
            SELECT c.*, u.name as creator_name, u.avatar_url as creator_avatar
            FROM conversations c
            LEFT JOIN users u ON c.created_by = u.id
            WHERE c.id = @GroupId AND c.is_group = true";

        return await _dbConnection.QuerySingleOrDefaultAsync<Conversation>(sql, new { GroupId = groupId });
    }

    public async Task<GroupInfoResponse?> GetGroupInfoAsync(int groupId, int currentUserId)
    {
        var sql = @"
            SELECT 
                c.id, c.name, c.description, c.avatar_url, c.max_members, 
                c.is_public, c.require_approval, c.invite_link, c.created_at,
                u.id as creator_id, u.name as creator_name, u.avatar_url as creator_avatar,
                cp.role as user_role,
                (SELECT COUNT(*) FROM conversation_participants WHERE conversation_id = c.id) as member_count,
                (SELECT COUNT(*) FROM conversation_participants cp2 JOIN users u2 ON cp2.user_id = u2.id WHERE cp2.conversation_id = c.id AND u2.is_active = true) as online_count
            FROM conversations c
            LEFT JOIN users u ON c.created_by = u.id
            LEFT JOIN conversation_participants cp ON c.id = cp.conversation_id AND cp.user_id = @UserId
            WHERE c.id = @GroupId AND c.is_group = true";

        var result = await _dbConnection.QuerySingleOrDefaultAsync<GroupInfoQueryResult>(sql, new { GroupId = groupId, UserId = currentUserId });

        if (result == null) return null;

        return new GroupInfoResponse
        {
            id = result.id,
            name = result.name,
            description = result.description,
            avatar_url = result.avatar_url,
            max_members = result.max_members,
            is_public = result.is_public,
            require_approval = result.require_approval,
            invite_link = result.invite_link,
            created_at = result.created_at,
            created_by = new UserResponse
            {
                id = result.creator_id ?? 0,
                name = result.creator_name ?? "",
                avatar_url = result.creator_avatar
            },
            user_role = result.user_role,
            member_count = result.member_count,
            online_count = result.online_count
        };
    }

    public async Task<bool> UpdateGroupAsync(int groupId, UpdateGroupRequest request)
    {
        var updateFields = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(request.name))
            updateFields["name"] = request.name;

        if (request.description != null)
            updateFields["description"] = request.description;

        if (request.avatar_url != null)
            updateFields["avatar_url"] = request.avatar_url;

        if (request.is_public.HasValue)
            updateFields["is_public"] = request.is_public.Value;

        if (request.require_approval.HasValue)
            updateFields["require_approval"] = request.require_approval.Value;

        if (request.max_members.HasValue)
            updateFields["max_members"] = request.max_members.Value;

        if (updateFields.Count == 0) return true;

        return await UpdatePartialAsync(groupId, updateFields);
    }

    public async Task<bool> DeleteGroupAsync(int groupId)
    {
        // This will cascade delete all related data due to database constraints
        // Note: Base DeleteAsync only checks by ID, so make sure groupId is a valid group
        return await DeleteAsync(groupId);
    }

    public async Task<List<Conversation>> GetUserGroupsAsync(int userId)
    {
        var sql = @"
            SELECT c.*
            FROM conversations c
            INNER JOIN conversation_participants cp ON c.id = cp.conversation_id
            WHERE cp.user_id = @UserId AND c.is_group = true
            ORDER BY c.created_at DESC";

        var result = await _dbConnection.QueryAsync<Conversation>(sql, new { UserId = userId });
        return result.ToList();
    }

    public async Task<List<GroupInfoResponse>> GetUserGroupsWithInfoAsync(int userId)
    {
        var sql = @"
            SELECT 
                c.id, c.name, c.description, c.avatar_url, c.max_members, 
                c.is_public, c.require_approval, c.invite_link, c.created_at,
                cp.role as user_role,
                (SELECT COUNT(*) FROM conversation_participants WHERE conversation_id = c.id) as member_count,
                (SELECT COUNT(*) FROM conversation_participants cp2 JOIN users u2 ON cp2.user_id = u2.id WHERE cp2.conversation_id = c.id AND u2.is_active = true) as online_count
            FROM conversations c
            INNER JOIN conversation_participants cp ON c.id = cp.conversation_id
            WHERE cp.user_id = @UserId AND c.is_group = true
            ORDER BY c.created_at DESC";

        var results = await _dbConnection.QueryAsync<UserGroupsQueryResult>(sql, new { UserId = userId });

        return results.Select(r => new GroupInfoResponse
        {
            id = r.id,
            name = r.name,
            description = r.description,
            avatar_url = r.avatar_url,
            max_members = r.max_members,
            is_public = r.is_public,
            require_approval = r.require_approval,
            invite_link = r.invite_link,
            created_at = r.created_at,
            user_role = r.user_role,
            member_count = r.member_count,
            online_count = r.online_count
        }).ToList();
    }

    // Member management
    public async Task<List<GroupMemberResponse>> GetGroupMembersAsync(int groupId)
    {
        var sql = @"
            SELECT 
                u.id as user_id, u.name, u.user_name, u.email, u.avatar_url,
                cp.role, cp.joined_at, cp.last_seen_at, u.is_active as is_online
            FROM conversation_participants cp
            INNER JOIN users u ON cp.user_id = u.id
            WHERE cp.conversation_id = @GroupId
            ORDER BY 
                CASE cp.role 
                    WHEN 'admin' THEN 1 
                    WHEN 'moderator' THEN 2 
                    ELSE 3 
                END,
                cp.joined_at";

        var results = await _dbConnection.QueryAsync<GroupMemberResponse>(sql, new { GroupId = groupId });
        return results.ToList();
    }

    public async Task<GroupMemberResponse?> GetMemberAsync(int groupId, int userId)
    {
        var sql = @"
            SELECT 
                u.id as user_id, u.name, u.user_name, u.email, u.avatar_url,
                cp.role, cp.joined_at, cp.last_seen_at, u.is_active as is_online
            FROM conversation_participants cp
            INNER JOIN users u ON cp.user_id = u.id
            WHERE cp.conversation_id = @GroupId AND cp.user_id = @UserId";

        return await _dbConnection.QuerySingleOrDefaultAsync<GroupMemberResponse>(sql, new { GroupId = groupId, UserId = userId });
    }

    public async Task<bool> AddMemberAsync(int groupId, int userId, string role = "member")
    {
        var participant = new ConversationParticipant
        {
            conversation_id = groupId,
            user_id = userId,
            joined_at = DateTime.UtcNow,
            role = role
        };

        var id = await _participantRepo.InsertAsync(participant);
        return id > 0;
    }

    public async Task<bool> RemoveMemberAsync(int groupId, int userId)
    {
        var sql = "DELETE FROM conversation_participants WHERE conversation_id = @GroupId AND user_id = @UserId";
        var affected = await _dbConnection.ExecuteAsync(sql, new { GroupId = groupId, UserId = userId });
        return affected > 0;
    }

    public async Task<bool> UpdateMemberRoleAsync(int groupId, int userId, string role)
    {
        var sql = @"
            UPDATE conversation_participants 
            SET role = @Role 
            WHERE conversation_id = @GroupId AND user_id = @UserId";

        var affected = await _dbConnection.ExecuteAsync(sql, new { GroupId = groupId, UserId = userId, Role = role });
        return affected > 0;
    }

    public async Task<bool> IsUserInGroupAsync(int groupId, int userId)
    {
        var sql = @"
            SELECT COUNT(1) FROM conversation_participants 
            WHERE conversation_id = @GroupId AND user_id = @UserId";

        var count = await _dbConnection.QuerySingleAsync<int>(sql, new { GroupId = groupId, UserId = userId });
        return count > 0;
    }

    public async Task<string?> GetUserRoleInGroupAsync(int groupId, int userId)
    {
        var sql = @"
            SELECT role FROM conversation_participants 
            WHERE conversation_id = @GroupId AND user_id = @UserId";

        return await _dbConnection.QuerySingleOrDefaultAsync<string>(sql, new { GroupId = groupId, UserId = userId });
    }

    public async Task<bool> UpdateUserOnlineStatusAsync(int groupId, int userId, bool isOnline)
    {
        var sql = @"
            UPDATE conversation_participants 
            SET last_seen_at = @LastSeen
            WHERE conversation_id = @GroupId AND user_id = @UserId";

        var affected = await _dbConnection.ExecuteAsync(sql, new
        {
            GroupId = groupId,
            UserId = userId,
            IsOnline = isOnline,
            LastSeen = DateTime.UtcNow
        });

        return affected > 0;
    }

    public async Task<int> GetGroupMemberCountAsync(int groupId)
    {
        var sql = "SELECT COUNT(*) FROM conversation_participants WHERE conversation_id = @GroupId";
        return await _dbConnection.QuerySingleAsync<int>(sql, new { GroupId = groupId });
    }

    // Join requests
    public async Task<List<JoinRequestResponse>> GetPendingRequestsAsync(int groupId)
    {
        var sql = @"
            SELECT 
                gjr.id, gjr.user_id, gjr.message, gjr.status, gjr.requested_at,
                gjr.reviewed_by, gjr.reviewed_at, gjr.reason,
                u.id as user_id, u.name as user_name, u.email as user_email, u.avatar_url as user_avatar
            FROM group_join_requests gjr
            INNER JOIN users u ON gjr.user_id = u.id
            WHERE gjr.conversation_id = @GroupId AND gjr.status = 'pending'
            ORDER BY gjr.requested_at";

        var results = await _dbConnection.QueryAsync<PendingRequestsQueryResult>(sql, new { GroupId = groupId });

        return results.Select(r => new JoinRequestResponse
        {
            id = r.id,
            conversation_id = groupId,
            user_id = r.user_id,
            message = r.message,
            status = r.status,
            requested_at = r.requested_at,
            reviewed_by = r.reviewed_by,
            reviewed_at = r.reviewed_at,
            reason = r.reason,
            user = new UserResponse
            {
                id = r.user_id,
                name = r.user_name,
                email = r.user_email,
                avatar_url = r.user_avatar
            }
        }).ToList();
    }

    public async Task<JoinRequestResponse?> GetJoinRequestAsync(int requestId)
    {
        var sql = @"
            SELECT 
                gjr.id, gjr.user_id, gjr.message, gjr.status, gjr.requested_at,
                gjr.reviewed_by, gjr.reviewed_at, gjr.reason,
                u.id as user_id, u.name as user_name, u.email as user_email, u.avatar_url as user_avatar,
                r.name as reviewer_name
            FROM group_join_requests gjr
            INNER JOIN users u ON gjr.user_id = u.id
            LEFT JOIN users r ON gjr.reviewed_by = r.id
            WHERE gjr.id = @RequestId";

        var result = await _dbConnection.QuerySingleOrDefaultAsync<JoinRequestQueryResult>(sql, new { RequestId = requestId });

        if (result == null) return null;

        return new JoinRequestResponse
        {
            id = result.id,
            conversation_id = result.conversation_id,
            user_id = result.user_id,
            message = result.message,
            status = result.status,
            requested_at = result.requested_at,
            reviewed_by = result.reviewed_by,
            reviewed_at = result.reviewed_at,
            reason = result.reason,
            user = new UserResponse
            {
                id = result.user_id,
                name = result.user_name,
                email = result.user_email,
                avatar_url = result.user_avatar
            },
            reviewer = result.reviewed_by != null ? new UserResponse
            {
                id = result.reviewed_by ?? 0,
                name = result.reviewer_name ?? ""
            } : null
        };
    }

    public async Task<int> CreateJoinRequestAsync(int groupId, int userId, string? message)
    {
        var joinRequest = new GroupJoinRequest
        {
            conversation_id = groupId,
            user_id = userId,
            message = message,
            requested_at = DateTime.UtcNow,
            status = "pending"
        };

        return await _joinRequestRepo.InsertAsync(joinRequest);
    }

    public async Task<bool> HandleJoinRequestAsync(int requestId, string action, int reviewedBy, string? reason)
    {
        var updateFields = new Dictionary<string, object>
        {
            ["status"] = action == "approve" ? "approved" : "rejected",
            ["reviewed_by"] = reviewedBy,
            ["reviewed_at"] = DateTime.UtcNow,
            ["reason"] = reason
        };

        return await _joinRequestRepo.UpdatePartialAsync(requestId, updateFields);
    }

    public async Task<bool> HasPendingRequestAsync(int groupId, int userId)
    {
        var sql = @"
            SELECT COUNT(1) FROM group_join_requests 
            WHERE conversation_id = @GroupId AND user_id = @UserId AND status = 'pending'";

        var count = await _dbConnection.QuerySingleAsync<int>(sql, new { GroupId = groupId, UserId = userId });
        return count > 0;
    }

    // Invite links
    public async Task<string> GetOrCreateInviteLinkAsync(int groupId)
    {
        var sql = "SELECT invite_link FROM conversations WHERE id = @GroupId";
        var existingLink = await _dbConnection.QuerySingleOrDefaultAsync<string>(sql, new { GroupId = groupId });

        if (!string.IsNullOrEmpty(existingLink))
        {
            return existingLink;
        }

        var newCode = await GenerateUniqueInviteCodeAsync();
        await UpdatePartialAsync(groupId, new { invite_link = newCode });

        return newCode;
    }

    public async Task<int?> GetGroupByInviteLinkAsync(string inviteLink)
    {
        var sql = "SELECT id FROM conversations WHERE invite_link = @InviteLink AND is_group = true";
        return await _dbConnection.QuerySingleOrDefaultAsync<int?>(sql, new { InviteLink = inviteLink });
    }

    public async Task<bool> RegenerateInviteLinkAsync(int groupId)
    {
        var newCode = await GenerateUniqueInviteCodeAsync();
        return await UpdatePartialAsync(groupId, new { invite_link = newCode });
    }

    // Permissions
    public async Task<bool> HasPermissionAsync(int groupId, int userId, string permission)
    {
        var userRole = await GetUserRoleInGroupAsync(groupId, userId);
        if (userRole == null) return false;

        // Admins always have all permissions
        if (userRole == "admin") return true;

        var sql = @"
            SELECT COUNT(1) FROM group_permissions 
            WHERE conversation_id = @GroupId 
            AND permission_type = @Permission 
            AND (required_role = @UserRole OR 
                 (required_role = 'member' AND @UserRole IN ('moderator', 'admin')) OR
                 (required_role = 'moderator' AND @UserRole = 'admin'))";

        var hasPermission = await _dbConnection.QuerySingleAsync<int>(sql, new
        {
            GroupId = groupId,
            Permission = permission,
            UserRole = userRole
        });

        return hasPermission > 0;
    }

    public async Task<List<string>> GetUserPermissionsAsync(int groupId, int userId)
    {
        var userRole = await GetUserRoleInGroupAsync(groupId, userId);
        if (userRole == null) return new List<string>();

        if (userRole == "admin")
        {
            // Admins have all permissions
            var allPermissionsSql = "SELECT DISTINCT permission_type FROM group_permissions WHERE conversation_id = @GroupId";
            var allPermissions = await _dbConnection.QueryAsync<string>(allPermissionsSql, new { GroupId = groupId });
            return allPermissions.ToList();
        }

        var sql = @"
            SELECT DISTINCT permission_type FROM group_permissions 
            WHERE conversation_id = @GroupId 
            AND (required_role = @UserRole OR 
                 (required_role = 'member' AND @UserRole IN ('moderator', 'admin')) OR
                 (required_role = 'moderator' AND @UserRole = 'admin'))";

        var permissions = await _dbConnection.QueryAsync<string>(sql, new { GroupId = groupId, UserRole = userRole });
        return permissions.ToList();
    }

    public async Task<List<GroupPermission>> GetGroupPermissionsAsync(int groupId)
    {
        var sql = "SELECT * FROM group_permissions WHERE conversation_id = @GroupId ORDER BY permission_type";
        var permissions = await _dbConnection.QueryAsync<GroupPermission>(sql, new { GroupId = groupId });
        return permissions.ToList();
    }

    public async Task<bool> UpdateGroupPermissionsAsync(int groupId, List<GroupPermission> permissions)
    {
        using var transaction = _dbConnection.BeginTransaction();
        try
        {
            // Delete existing permissions
            var deleteSql = "DELETE FROM group_permissions WHERE conversation_id = @GroupId";
            await _dbConnection.ExecuteAsync(deleteSql, new { GroupId = groupId }, transaction);

            // Insert new permissions
            foreach (var permission in permissions)
            {
                var insertSql = @"
                    INSERT INTO group_permissions (conversation_id, permission_type, required_role, created_at)
                    VALUES (@ConversationId, @PermissionType, @RequiredRole, @CreatedAt)";

                await _dbConnection.ExecuteAsync(insertSql, new
                {
                    ConversationId = groupId,
                    PermissionType = permission.permission_type,
                    RequiredRole = permission.required_role,
                    CreatedAt = DateTime.UtcNow
                }, transaction);
            }

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            return false;
        }
    }

    // Statistics and utility methods
    public async Task<Dictionary<string, int>> GetGroupStatsAsync(int groupId)
    {
        var sql = @"
            SELECT 
                COUNT(*) as total_members,
                COUNT(CASE WHEN u.is_active = true THEN 1 END) as online_members,
                COUNT(CASE WHEN role = 'admin' THEN 1 END) as admin_count,
                COUNT(CASE WHEN role = 'moderator' THEN 1 END) as moderator_count
            FROM conversation_participants 
            WHERE conversation_id = @GroupId";

        var stats = await _dbConnection.QuerySingleAsync<GroupStatsQueryResult>(sql, new { GroupId = groupId });

        return new Dictionary<string, int>
        {
            ["total_members"] = stats.total_members,
            ["online_members"] = stats.online_members,
            ["admin_count"] = stats.admin_count,
            ["moderator_count"] = stats.moderator_count
        };
    }

    public async Task<List<int>> GetOnlineMembersAsync(int groupId)
    {
        var sql = @"
            SELECT cp.user_id FROM conversation_participants cp
            INNER JOIN users u ON cp.user_id = u.id
            WHERE cp.conversation_id = @GroupId AND u.is_active = true";

        var userIds = await _dbConnection.QueryAsync<int>(sql, new { GroupId = groupId });
        return userIds.ToList();
    }

    public async Task<bool> CanUserJoinGroupAsync(int groupId, int userId)
    {
        // Check if user is already a member
        if (await IsUserInGroupAsync(groupId, userId))
            return false;

        // Check if group exists and get max members
        var group = await GetGroupByIdAsync(groupId);
        if (group == null) return false;

        // Check current member count
        var currentCount = await GetGroupMemberCountAsync(groupId);
        if (currentCount >= group.max_members) return false;

        return true;
    }

    public async Task<string> GenerateUniqueInviteCodeAsync()
    {
        string code;
        bool exists;
        int attempts = 0;
        const int maxAttempts = 10;

        do
        {
            code = GenerateRandomCode();
            var sql = "SELECT COUNT(1) FROM conversations WHERE invite_link = @Code";
            var count = await _dbConnection.QuerySingleAsync<int>(sql, new { Code = code });
            exists = count > 0;
            attempts++;
        }
        while (exists && attempts < maxAttempts);

        if (exists)
        {
            throw new InvalidOperationException("Failed to generate unique invite code after multiple attempts");
        }

        return code;
    }

    private static string GenerateRandomCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        using var rng = RandomNumberGenerator.Create();
        var result = new char[8];
        var bytes = new byte[8];

        rng.GetBytes(bytes);

        for (int i = 0; i < 8; i++)
        {
            result[i] = chars[bytes[i] % chars.Length];
        }

        return new string(result);
    }
}