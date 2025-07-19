using ChatServer.Applications;
using ChatServer.Models;
using ChatServer.Repositories.Group;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatServer.Controllers;

[ApiController]
[Route("api/groups")]
[Authorize]
public class GroupsController : BaseApiController
{
    private readonly IGroupRepository _groupRepo;
    private readonly IChatClientNotifier _notifier;
    private readonly ILogger<GroupsController> _logger;

    public GroupsController(
        IGroupRepository groupRepo, 
        IChatClientNotifier notifier,
        ILogger<GroupsController> logger)
    {
        _groupRepo = groupRepo;
        _notifier = notifier;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException("Invalid user ID");
    }

    // POST /api/groups - Create new group
    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            // Validate request
            if (string.IsNullOrWhiteSpace(request.name))
            {
                return BadRequestResponse("Group name is required");
            }

            if (request.max_members < 2 || request.max_members > 1000)
            {
                return BadRequestResponse("Max members must be between 2 and 1000");
            }

            // Create group
            var group = await _groupRepo.CreateGroupAsync(request, userId);
            
            // Add creator as admin
            await _groupRepo.AddMemberAsync(group.id, userId, "admin");
            
            // Add initial members
            var addedMembers = new List<GroupMemberResponse>();
            foreach (var memberId in request.initial_members)
            {
                if (memberId != userId) // Don't add creator twice
                {
                    var success = await _groupRepo.AddMemberAsync(group.id, memberId, "member");
                    if (success)
                    {
                        var member = await _groupRepo.GetMemberAsync(group.id, memberId);
                        if (member != null)
                        {
                            addedMembers.Add(member);
                        }
                    }
                }
            }
            
            // Get complete group info
            var groupInfo = await _groupRepo.GetGroupInfoAsync(group.id, userId);
            
            _logger.LogInformation("Group created: {GroupId} by user {UserId}", group.id, userId);
            
            // Notify via SignalR (will be implemented when we extend the notifier)
            // await _notifier.GroupCreated(groupInfo);
            
            return OkResponse(groupInfo, "Group created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group");
            return InternalErrorResponse("Failed to create group");
        }
    }

    // PUT /api/groups/{id} - Update group info
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateGroup(int id, [FromBody] UpdateGroupRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            // Check if user is in group and has permission
            if (!await _groupRepo.IsUserInGroupAsync(id, userId))
            {
                return BadRequestResponse("You are not a member of this group");
            }

            if (!await _groupRepo.HasPermissionAsync(id, userId, "edit_info"))
            {
                return BadRequestResponse("Insufficient permissions to edit group info");
            }
            
            var updated = await _groupRepo.UpdateGroupAsync(id, request);
            if (updated)
            {
                var group = await _groupRepo.GetGroupInfoAsync(id, userId);
                
                _logger.LogInformation("Group {GroupId} updated by user {UserId}", id, userId);
                
                // await _notifier.GroupUpdated(group);
                return OkResponse(group, "Group updated successfully");
            }
            
            return BadRequestResponse("Failed to update group");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating group {GroupId}", id);
            return InternalErrorResponse("Failed to update group");
        }
    }

    // GET /api/groups/{id}/info - Get group details
    [HttpGet("{id}/info")]
    public async Task<IActionResult> GetGroupInfo(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            // Check if user is member or if group is public
            var group = await _groupRepo.GetGroupByIdAsync(id);
            if (group == null)
            {
                return NotFoundResponse("Group not found");
            }

            var isUserInGroup = await _groupRepo.IsUserInGroupAsync(id, userId);
            if (!isUserInGroup && !group.is_public)
            {
                return BadRequestResponse("You are not a member of this group");
            }
            
            var groupInfo = await _groupRepo.GetGroupInfoAsync(id, userId);
            return OkResponse(groupInfo, "Group info retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting group info {GroupId}", id);
            return InternalErrorResponse("Failed to get group info");
        }
    }

    // GET /api/groups - Get user's groups
    [HttpGet]
    public async Task<IActionResult> GetUserGroups()
    {
        try
        {
            var userId = GetCurrentUserId();
            var groups = await _groupRepo.GetUserGroupsWithInfoAsync(userId);
            
            return OkResponse(groups, "User groups retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user groups for user {UserId}", GetCurrentUserId());
            return InternalErrorResponse("Failed to get user groups");
        }
    }

    // POST /api/groups/{id}/members - Add members to group
    [HttpPost("{id}/members")]
    public async Task<IActionResult> AddMembers(int id, [FromBody] AddMembersRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            if (!await _groupRepo.IsUserInGroupAsync(id, userId))
            {
                return BadRequestResponse("You are not a member of this group");
            }

            if (!await _groupRepo.HasPermissionAsync(id, userId, "add_members"))
            {
                return BadRequestResponse("Insufficient permissions to add members");
            }

            // Check group capacity
            var group = await _groupRepo.GetGroupByIdAsync(id);
            if (group == null)
            {
                return NotFoundResponse("Group not found");
            }

            var currentMemberCount = await _groupRepo.GetGroupMemberCountAsync(id);
            if (currentMemberCount + request.user_ids.Count > group.max_members)
            {
                return BadRequestResponse($"Adding these members would exceed the group limit of {group.max_members}");
            }
            
            var addedMembers = new List<GroupMemberResponse>();
            foreach (var memberId in request.user_ids)
            {
                if (!await _groupRepo.IsUserInGroupAsync(id, memberId))
                {
                    var success = await _groupRepo.AddMemberAsync(id, memberId, "member");
                    if (success)
                    {
                        var member = await _groupRepo.GetMemberAsync(id, memberId);
                        if (member != null)
                        {
                            addedMembers.Add(member);
                            
                            _logger.LogInformation("User {MemberId} added to group {GroupId} by {UserId}", 
                                memberId, id, userId);
                            
                            // await _notifier.GroupMemberAdded(id, member);
                        }
                    }
                }
            }
            
            return OkResponse(addedMembers, $"Added {addedMembers.Count} members successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding members to group {GroupId}", id);
            return InternalErrorResponse("Failed to add members");
        }
    }

    // DELETE /api/groups/{id}/members/{userId} - Remove member
    [HttpDelete("{id}/members/{targetUserId}")]
    public async Task<IActionResult> RemoveMember(int id, int targetUserId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            
            if (!await _groupRepo.IsUserInGroupAsync(id, currentUserId))
            {
                return BadRequestResponse("You are not a member of this group");
            }

            // Can remove self or if have permission
            if (currentUserId != targetUserId && !await _groupRepo.HasPermissionAsync(id, currentUserId, "remove_members"))
            {
                return BadRequestResponse("Insufficient permissions to remove members");
            }

            // Check if target user is in group
            if (!await _groupRepo.IsUserInGroupAsync(id, targetUserId))
            {
                return BadRequestResponse("Target user is not a member of this group");
            }

            // Prevent removing the last admin
            var targetRole = await _groupRepo.GetUserRoleInGroupAsync(id, targetUserId);
            if (targetRole == "admin")
            {
                var stats = await _groupRepo.GetGroupStatsAsync(id);
                if (stats["admin_count"] <= 1)
                {
                    return BadRequestResponse("Cannot remove the last admin from the group");
                }
            }
            
            var success = await _groupRepo.RemoveMemberAsync(id, targetUserId);
            if (success)
            {
                _logger.LogInformation("User {TargetUserId} removed from group {GroupId} by {CurrentUserId}", 
                    targetUserId, id, currentUserId);
                
                var reason = currentUserId == targetUserId ? "left" : "removed";
                // await _notifier.GroupMemberRemoved(id, targetUserId, reason);
                
                return OkResponse(true, "Member removed successfully");
            }
            
            return BadRequestResponse("Failed to remove member");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing member {TargetUserId} from group {GroupId}", targetUserId, id);
            return InternalErrorResponse("Failed to remove member");
        }
    }

    // POST /api/groups/{id}/leave - Leave group
    [HttpPost("{id}/leave")]
    public async Task<IActionResult> LeaveGroup(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            if (!await _groupRepo.IsUserInGroupAsync(id, userId))
            {
                return BadRequestResponse("You are not a member of this group");
            }

            // Check if user is the last admin
            var userRole = await _groupRepo.GetUserRoleInGroupAsync(id, userId);
            if (userRole == "admin")
            {
                var stats = await _groupRepo.GetGroupStatsAsync(id);
                if (stats["admin_count"] <= 1 && stats["total_members"] > 1)
                {
                    return BadRequestResponse("You are the last admin. Please promote another member to admin before leaving");
                }
            }
            
            var success = await _groupRepo.RemoveMemberAsync(id, userId);
            
            if (success)
            {
                _logger.LogInformation("User {UserId} left group {GroupId}", userId, id);
                
                // await _notifier.GroupMemberRemoved(id, userId, "left");
                return OkResponse(true, "Left group successfully");
            }
            
            return BadRequestResponse("Failed to leave group");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving group {GroupId} for user {UserId}", id, GetCurrentUserId());
            return InternalErrorResponse("Failed to leave group");
        }
    }

    // GET /api/groups/{id}/members - Get group members
    [HttpGet("{id}/members")]
    public async Task<IActionResult> GetMembers(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            // Check if user is member or if group is public
            var group = await _groupRepo.GetGroupByIdAsync(id);
            if (group == null)
            {
                return NotFoundResponse("Group not found");
            }

            var isUserInGroup = await _groupRepo.IsUserInGroupAsync(id, userId);
            if (!isUserInGroup && !group.is_public)
            {
                return BadRequestResponse("You are not a member of this group");
            }
            
            var members = await _groupRepo.GetGroupMembersAsync(id);
            return OkResponse(members, "Group members retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting members for group {GroupId}", id);
            return InternalErrorResponse("Failed to get group members");
        }
    }

    // POST /api/groups/{id}/promote - Promote member to admin/moderator
    [HttpPost("{id}/promote")]
    public async Task<IActionResult> PromoteMember(int id, [FromBody] PromoteMemberRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            if (!await _groupRepo.IsUserInGroupAsync(id, userId))
            {
                return BadRequestResponse("You are not a member of this group");
            }

            if (!await _groupRepo.HasPermissionAsync(id, userId, "promote_members"))
            {
                return BadRequestResponse("Insufficient permissions to promote members");
            }

            // Validate role
            if (!new[] { "admin", "moderator", "member" }.Contains(request.role))
            {
                return BadRequestResponse("Invalid role. Must be admin, moderator, or member");
            }

            // Check if target user is in group
            if (!await _groupRepo.IsUserInGroupAsync(id, request.user_id))
            {
                return BadRequestResponse("Target user is not a member of this group");
            }
            
            var success = await _groupRepo.UpdateMemberRoleAsync(id, request.user_id, request.role);
            if (success)
            {
                _logger.LogInformation("User {TargetUserId} promoted to {Role} in group {GroupId} by {UserId}", 
                    request.user_id, request.role, id, userId);
                
                // await _notifier.GroupMemberPromoted(id, request.user_id, request.role);
                return OkResponse(true, $"Member promoted to {request.role} successfully");
            }
            
            return BadRequestResponse("Failed to promote member");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error promoting member {TargetUserId} in group {GroupId}", request.user_id, id);
            return InternalErrorResponse("Failed to promote member");
        }
    }

    // GET /api/groups/{id}/invite-link - Get or generate invite link
    [HttpGet("{id}/invite-link")]
    public async Task<IActionResult> GetInviteLink(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            if (!await _groupRepo.IsUserInGroupAsync(id, userId))
            {
                return BadRequestResponse("You are not a member of this group");
            }

            if (!await _groupRepo.HasPermissionAsync(id, userId, "manage_invites"))
            {
                return BadRequestResponse("Insufficient permissions to manage invites");
            }
            
            var link = await _groupRepo.GetOrCreateInviteLinkAsync(id);
            return OkResponse(new { invite_link = link }, "Invite link retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invite link for group {GroupId}", id);
            return InternalErrorResponse("Failed to get invite link");
        }
    }

    // POST /api/groups/{id}/regenerate-invite - Regenerate invite link
    [HttpPost("{id}/regenerate-invite")]
    public async Task<IActionResult> RegenerateInviteLink(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            if (!await _groupRepo.IsUserInGroupAsync(id, userId))
            {
                return BadRequestResponse("You are not a member of this group");
            }

            if (!await _groupRepo.HasPermissionAsync(id, userId, "manage_invites"))
            {
                return BadRequestResponse("Insufficient permissions to manage invites");
            }
            
            var success = await _groupRepo.RegenerateInviteLinkAsync(id);
            if (success)
            {
                var newLink = await _groupRepo.GetOrCreateInviteLinkAsync(id);
                
                _logger.LogInformation("Invite link regenerated for group {GroupId} by user {UserId}", id, userId);
                
                return OkResponse(new { invite_link = newLink }, "Invite link regenerated successfully");
            }
            
            return BadRequestResponse("Failed to regenerate invite link");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating invite link for group {GroupId}", id);
            return InternalErrorResponse("Failed to regenerate invite link");
        }
    }

    // POST /api/groups/join/{inviteCode} - Join group via invite link
    [HttpPost("join/{inviteCode}")]
    public async Task<IActionResult> JoinViaInvite(string inviteCode)
    {
        try
        {
            var userId = GetCurrentUserId();
            var groupId = await _groupRepo.GetGroupByInviteLinkAsync(inviteCode);
            
            if (groupId == null)
            {
                return BadRequestResponse("Invalid or expired invite link");
            }
            
            if (await _groupRepo.IsUserInGroupAsync(groupId.Value, userId))
            {
                return BadRequestResponse("You are already a member of this group");
            }

            if (!await _groupRepo.CanUserJoinGroupAsync(groupId.Value, userId))
            {
                return BadRequestResponse("Cannot join group (may be full or other restrictions)");
            }
            
            var group = await _groupRepo.GetGroupByIdAsync(groupId.Value);
            if (group == null)
            {
                return BadRequestResponse("Group not found");
            }
            
            if (group.require_approval)
            {
                // Create join request
                var hasExistingRequest = await _groupRepo.HasPendingRequestAsync(groupId.Value, userId);
                if (hasExistingRequest)
                {
                    return BadRequestResponse("You already have a pending join request for this group");
                }
                
                var requestId = await _groupRepo.CreateJoinRequestAsync(groupId.Value, userId, null);
                
                _logger.LogInformation("Join request created for user {UserId} to group {GroupId}", userId, groupId.Value);
                
                // await _notifier.GroupJoinRequest(groupId.Value, joinRequest);
                
                return OkResponse(new { message = "Join request sent. Waiting for admin approval.", request_id = requestId });
            }
            else
            {
                // Direct join
                var success = await _groupRepo.AddMemberAsync(groupId.Value, userId, "member");
                if (success)
                {
                    var member = await _groupRepo.GetMemberAsync(groupId.Value, userId);
                    
                    _logger.LogInformation("User {UserId} joined group {GroupId} via invite", userId, groupId.Value);
                    
                    // await _notifier.GroupMemberAdded(groupId.Value, member);
                    
                    return OkResponse(new { message = "Joined group successfully", group_id = groupId.Value });
                }
            }
            
            return BadRequestResponse("Failed to join group");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining group via invite {InviteCode}", inviteCode);
            return InternalErrorResponse("Failed to join group");
        }
    }

    // GET /api/groups/{id}/stats - Get group statistics
    [HttpGet("{id}/stats")]
    public async Task<IActionResult> GetGroupStats(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            if (!await _groupRepo.IsUserInGroupAsync(id, userId))
            {
                return BadRequestResponse("You are not a member of this group");
            }
            
            var stats = await _groupRepo.GetGroupStatsAsync(id);
            return OkResponse(stats, "Group statistics retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stats for group {GroupId}", id);
            return InternalErrorResponse("Failed to get group statistics");
        }
    }

    // DELETE /api/groups/{id} - Delete group (admin only)
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGroup(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            if (!await _groupRepo.IsUserInGroupAsync(id, userId))
            {
                return BadRequestResponse("You are not a member of this group");
            }

            var userRole = await _groupRepo.GetUserRoleInGroupAsync(id, userId);
            if (userRole != "admin")
            {
                return BadRequestResponse("Only admins can delete groups");
            }
            
            var success = await _groupRepo.DeleteGroupAsync(id);
            if (success)
            {
                _logger.LogInformation("Group {GroupId} deleted by user {UserId}", id, userId);
                
                // TODO: Notify all members about group deletion
                
                return OkResponse(true, "Group deleted successfully");
            }
            
            return BadRequestResponse("Failed to delete group");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting group {GroupId}", id);
            return InternalErrorResponse("Failed to delete group");
        }
    }
}