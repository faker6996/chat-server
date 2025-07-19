using ChatServer.Infrastructure.Services;
using ChatServer.Core.Models;
using ChatServer.Infrastructure.Repositories.Group;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatServer.Controllers;

[ApiController]
[Route("api/groups/{groupId}/requests")]
[Authorize]
public class GroupJoinRequestsController : BaseApiController
{
    private readonly IGroupRepository _groupRepo;
    private readonly IChatClientNotifier _notifier;
    private readonly ILogger<GroupJoinRequestsController> _logger;

    public GroupJoinRequestsController(
        IGroupRepository groupRepo,
        IChatClientNotifier notifier,
        ILogger<GroupJoinRequestsController> logger)
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

    // POST /api/groups/{groupId}/requests - Request to join group
    [HttpPost]
    public async Task<IActionResult> RequestJoin(int groupId, [FromBody] JoinRequestRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();

            // Check if group exists
            var group = await _groupRepo.GetGroupByIdAsync(groupId);
            if (group == null)
            {
                return NotFoundResponse("Group not found");
            }

            // Check if user is already a member
            if (await _groupRepo.IsUserInGroupAsync(groupId, userId))
            {
                return BadRequestResponse("You are already a member of this group");
            }

            // Check if user can join the group
            if (!await _groupRepo.CanUserJoinGroupAsync(groupId, userId))
            {
                return BadRequestResponse("Cannot join group (may be full or other restrictions)");
            }

            // Check if there's already a pending request
            if (await _groupRepo.HasPendingRequestAsync(groupId, userId))
            {
                return BadRequestResponse("You already have a pending join request for this group");
            }

            // Create join request
            var requestId = await _groupRepo.CreateJoinRequestAsync(groupId, userId, request.message);
            if (requestId > 0)
            {
                var joinRequest = await _groupRepo.GetJoinRequestAsync(requestId);

                _logger.LogInformation("Join request {RequestId} created for user {UserId} to group {GroupId}",
                    requestId, userId, groupId);

                // Notify group admins about the new join request
                // await _notifier.GroupJoinRequest(groupId, joinRequest);

                return OkResponse(joinRequest, "Join request submitted successfully");
            }

            return BadRequestResponse("Failed to create join request");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating join request for user {UserId} to group {GroupId}",
                GetCurrentUserId(), groupId);
            return InternalErrorResponse("Failed to create join request");
        }
    }

    // GET /api/groups/{groupId}/requests - Get pending requests (admins/moderators only)
    [HttpGet]
    public async Task<IActionResult> GetPendingRequests(int groupId)
    {
        try
        {
            var userId = GetCurrentUserId();

            // Check if user is in group
            if (!await _groupRepo.IsUserInGroupAsync(groupId, userId))
            {
                return BadRequestResponse("You are not a member of this group");
            }

            // Check permissions
            if (!await _groupRepo.HasPermissionAsync(groupId, userId, "manage_requests"))
            {
                return BadRequestResponse("Insufficient permissions to view join requests");
            }

            var requests = await _groupRepo.GetPendingRequestsAsync(groupId);
            return OkResponse(requests, "Pending requests retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending requests for group {GroupId}", groupId);
            return InternalErrorResponse("Failed to get pending requests");
        }
    }

    // PUT /api/groups/{groupId}/requests/{requestId} - Approve/reject request
    [HttpPut("{requestId}")]
    public async Task<IActionResult> HandleRequest(int groupId, int requestId, [FromBody] HandleRequestRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();

            // Validate action
            if (!new[] { "approve", "reject" }.Contains(request.action))
            {
                return BadRequestResponse("Action must be either 'approve' or 'reject'");
            }

            // Check if user is in group
            if (!await _groupRepo.IsUserInGroupAsync(groupId, userId))
            {
                return BadRequestResponse("You are not a member of this group");
            }

            // Check permissions
            if (!await _groupRepo.HasPermissionAsync(groupId, userId, "manage_requests"))
            {
                return BadRequestResponse("Insufficient permissions to handle join requests");
            }

            // Get the join request to validate it exists and belongs to this group
            var joinRequest = await _groupRepo.GetJoinRequestAsync(requestId);
            if (joinRequest == null)
            {
                return NotFoundResponse("Join request not found");
            }

            // Verify the request belongs to the specified group
            if (joinRequest.conversation_id != groupId)
            {
                return BadRequestResponse("Join request does not belong to this group");
            }

            // Check if request is still pending
            if (joinRequest.status != "pending")
            {
                return BadRequestResponse($"Join request has already been {joinRequest.status}");
            }

            // If approving, check if group has space and user can still join
            if (request.action == "approve")
            {
                if (await _groupRepo.IsUserInGroupAsync(groupId, joinRequest.user_id))
                {
                    return BadRequestResponse("User is already a member of the group");
                }

                if (!await _groupRepo.CanUserJoinGroupAsync(groupId, joinRequest.user_id))
                {
                    return BadRequestResponse("User cannot join group (may be full or other restrictions)");
                }
            }

            // Handle the request
            var success = await _groupRepo.HandleJoinRequestAsync(requestId, request.action, userId, request.reason);
            if (success)
            {
                _logger.LogInformation("Join request {RequestId} {Action} by user {UserId} for group {GroupId}",
                    requestId, request.action, userId, groupId);

                // If approved, add user to group
                if (request.action == "approve")
                {
                    var addSuccess = await _groupRepo.AddMemberAsync(groupId, joinRequest.user_id, "member");
                    if (addSuccess)
                    {
                        var member = await _groupRepo.GetMemberAsync(groupId, joinRequest.user_id);

                        _logger.LogInformation("User {NewMemberId} added to group {GroupId} via approved join request",
                            joinRequest.user_id, groupId);

                        // Notify about new member
                        // await _notifier.GroupMemberAdded(groupId, member);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to add user {UserId} to group {GroupId} after approving join request",
                            joinRequest.user_id, groupId);
                    }
                }

                // Notify about request handling
                // await _notifier.GroupJoinRequestHandled(requestId, request.action, request.reason);

                var updatedRequest = await _groupRepo.GetJoinRequestAsync(requestId);
                return OkResponse(updatedRequest, $"Join request {request.action}d successfully");
            }

            return BadRequestResponse("Failed to handle join request");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling join request {RequestId} for group {GroupId}", requestId, groupId);
            return InternalErrorResponse("Failed to handle join request");
        }
    }

    // GET /api/groups/{groupId}/requests/{requestId} - Get specific join request details
    [HttpGet("{requestId}")]
    public async Task<IActionResult> GetJoinRequest(int groupId, int requestId)
    {
        try
        {
            var userId = GetCurrentUserId();

            // Get the join request
            var joinRequest = await _groupRepo.GetJoinRequestAsync(requestId);
            if (joinRequest == null)
            {
                return NotFoundResponse("Join request not found");
            }

            // Verify the request belongs to the specified group
            if (joinRequest.conversation_id != groupId)
            {
                return BadRequestResponse("Join request does not belong to this group");
            }

            // Check if user can view this request
            // User can view if they are the requester or if they have manage_requests permission
            var canView = joinRequest.user_id == userId;

            if (!canView && await _groupRepo.IsUserInGroupAsync(groupId, userId))
            {
                canView = await _groupRepo.HasPermissionAsync(groupId, userId, "manage_requests");
            }

            if (!canView)
            {
                return BadRequestResponse("You don't have permission to view this join request");
            }

            return OkResponse(joinRequest, "Join request retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting join request {RequestId} for group {GroupId}", requestId, groupId);
            return InternalErrorResponse("Failed to get join request");
        }
    }

    // DELETE /api/groups/{groupId}/requests/{requestId} - Cancel join request (requester only)
    [HttpDelete("{requestId}")]
    public async Task<IActionResult> CancelJoinRequest(int groupId, int requestId)
    {
        try
        {
            var userId = GetCurrentUserId();

            // Get the join request
            var joinRequest = await _groupRepo.GetJoinRequestAsync(requestId);
            if (joinRequest == null)
            {
                return NotFoundResponse("Join request not found");
            }

            // Verify the request belongs to the specified group
            if (joinRequest.conversation_id != groupId)
            {
                return BadRequestResponse("Join request does not belong to this group");
            }

            // Check if user is the requester
            if (joinRequest.user_id != userId)
            {
                return BadRequestResponse("You can only cancel your own join requests");
            }

            // Check if request is still pending
            if (joinRequest.status != "pending")
            {
                return BadRequestResponse($"Cannot cancel a join request that has been {joinRequest.status}");
            }

            // Cancel the request by rejecting it
            var success = await _groupRepo.HandleJoinRequestAsync(requestId, "reject", userId, "Cancelled by requester");
            if (success)
            {
                _logger.LogInformation("Join request {RequestId} cancelled by requester {UserId} for group {GroupId}",
                    requestId, userId, groupId);

                return OkResponse(true, "Join request cancelled successfully");
            }

            return BadRequestResponse("Failed to cancel join request");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling join request {RequestId} for group {GroupId}", requestId, groupId);
            return InternalErrorResponse("Failed to cancel join request");
        }
    }

    // GET /api/groups/{groupId}/requests/my - Get current user's join request for the group
    [HttpGet("my")]
    public async Task<IActionResult> GetMyJoinRequest(int groupId)
    {
        try
        {
            var userId = GetCurrentUserId();

            // Check if group exists
            var group = await _groupRepo.GetGroupByIdAsync(groupId);
            if (group == null)
            {
                return NotFoundResponse("Group not found");
            }

            // If user is already a member, they don't have a join request
            if (await _groupRepo.IsUserInGroupAsync(groupId, userId))
            {
                return BadRequestResponse("You are already a member of this group");
            }

            // Check if there's a pending request for this user and group
            var hasPendingRequest = await _groupRepo.HasPendingRequestAsync(groupId, userId);

            if (hasPendingRequest)
            {
                // Find the pending request (this is a simplified approach)
                var pendingRequests = await _groupRepo.GetPendingRequestsAsync(groupId);
                var myRequest = pendingRequests.FirstOrDefault(r => r.user_id == userId);

                if (myRequest != null)
                {
                    return OkResponse(myRequest, "Your join request found");
                }
            }

            return NotFoundResponse("No join request found for this group");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user's join request for group {GroupId}", groupId);
            return InternalErrorResponse("Failed to get join request");
        }
    }

    // GET /api/groups/{groupId}/requests/history - Get all join requests for group (admins only)
    [HttpGet("history")]
    public async Task<IActionResult> GetRequestHistory(int groupId, [FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        try
        {
            var userId = GetCurrentUserId();

            // Check if user is in group and has permission
            if (!await _groupRepo.IsUserInGroupAsync(groupId, userId))
            {
                return BadRequestResponse("You are not a member of this group");
            }

            if (!await _groupRepo.HasPermissionAsync(groupId, userId, "manage_requests"))
            {
                return BadRequestResponse("Insufficient permissions to view request history");
            }

            // For now, we'll return pending requests
            // In a full implementation, this would include historical data with pagination
            var requests = await _groupRepo.GetPendingRequestsAsync(groupId);

            // If status filter is provided, we could filter here
            if (!string.IsNullOrEmpty(status))
            {
                requests = requests.Where(r => r.status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Simple pagination
            var skip = (page - 1) * limit;
            var paginatedRequests = requests.Skip(skip).Take(limit).ToList();

            var result = new
            {
                requests = paginatedRequests,
                total = requests.Count,
                page,
                limit,
                total_pages = (int)Math.Ceiling((double)requests.Count / limit)
            };

            return OkResponse(result, "Request history retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting request history for group {GroupId}", groupId);
            return InternalErrorResponse("Failed to get request history");
        }
    }
}