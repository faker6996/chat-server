using ChatServer.Controllers;
using ChatServer.Core.Models;
using ChatServer.Infrastructure.Services.GroupCall;
using Microsoft.AspNetCore.Mvc;

namespace ChatServer.Controllers;

[Route("api/groupcalls")]
public class GroupCallController : BaseApiController
{
    private readonly IGroupCallService _groupCallService;

    public GroupCallController(IGroupCallService groupCallService)
    {
        _groupCallService = groupCallService;
    }

    // GET /api/groupcalls/{groupId}/active
    [HttpGet("{groupId}/active")]
    public async Task<IActionResult> GetActiveGroupCall(int groupId)
    {
        try
        {
            var result = await _groupCallService.GetActiveGroupCallAsync(groupId);

            if (!result.IsSuccess)
            {
                return BadRequestResponse(result.ErrorMessage ?? "Unknown error");
            }

            if (result.Data == null)
            {
                return OkResponse("No active call found");
            }

            return OkResponse(result.Data, "Active group call retrieved successfully");
        }
        catch (Exception ex)
        {
            return InternalErrorResponse($"Failed to get active group call: {ex.Message}");
        }
    }

    // POST /api/groupcalls/{groupId}/start  
    [HttpPost("{groupId}/start")]
    public async Task<IActionResult> StartGroupCall(int groupId, [FromBody] StartGroupCallRequest? request = null)
    {
        try
        {
            // Fallback request if none provided
            request ??= new StartGroupCallRequest { call_type = "video" };
            
            // Get user ID from JWT token
            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return BadRequestResponse("User not authenticated");
            }

            var result = await _groupCallService.StartGroupCallAsync(groupId, userId.Value, request);

            if (!result.IsSuccess)
            {
                return BadRequestResponse(result.ErrorMessage ?? "Unknown error");
            }

            return OkResponse(result.Data, "Group call started successfully");
        }
        catch (Exception ex)
        {
            return InternalErrorResponse($"Failed to start group call: {ex.Message}");
        }
    }

    // GET /api/groupcalls/{callId}/participants
    [HttpGet("{callId}/participants")]
    public async Task<IActionResult> GetCallParticipants(string callId)
    {
        try
        {
            var result = await _groupCallService.GetCallParticipantsAsync(callId);

            if (!result.IsSuccess)
            {
                return BadRequestResponse(result.ErrorMessage ?? "Unknown error");
            }

            return OkResponse(result.Data, "Call participants retrieved successfully");
        }
        catch (Exception ex)
        {
            return InternalErrorResponse($"Failed to get call participants: {ex.Message}");
        }
    }

    // POST /api/groupcalls/{callId}/join
    [HttpPost("{callId}/join")]
    public async Task<IActionResult> JoinGroupCall(string callId, [FromBody] JoinGroupCallRequest request)
    {
        try
        {
            // Get user ID from JWT token
            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return BadRequestResponse("User not authenticated");
            }

            var result = await _groupCallService.JoinGroupCallAsync(callId, userId.Value, request);

            if (!result.IsSuccess)
            {
                return BadRequestResponse(result.ErrorMessage ?? "Unknown error");
            }

            return OkResponse(result.Data, "Joined group call successfully");
        }
        catch (Exception ex)
        {
            return InternalErrorResponse($"Failed to join group call: {ex.Message}");
        }
    }

    // DELETE /api/groupcalls/{callId}/leave
    [HttpDelete("{callId}/leave")]
    public async Task<IActionResult> LeaveGroupCall(string callId)
    {
        try
        {
            // Get user ID from JWT token
            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return BadRequestResponse("User not authenticated");
            }

            var result = await _groupCallService.LeaveGroupCallAsync(callId, userId.Value);

            if (!result.IsSuccess)
            {
                return BadRequestResponse(result.ErrorMessage ?? "Unknown error");
            }

            return OkResponse("Left group call successfully");
        }
        catch (Exception ex)
        {
            return InternalErrorResponse($"Failed to leave group call: {ex.Message}");
        }
    }

    // DELETE /api/groupcalls/{callId}/end
    [HttpDelete("{callId}/end")]
    public async Task<IActionResult> EndGroupCall(string callId)
    {
        try
        {
            // Get user ID from JWT token
            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return BadRequestResponse("User not authenticated");
            }

            var result = await _groupCallService.EndGroupCallAsync(callId, userId.Value);

            if (!result.IsSuccess)
            {
                return BadRequestResponse(result.ErrorMessage ?? "Unknown error");
            }

            return OkResponse("Group call ended successfully");
        }
        catch (Exception ex)
        {
            return InternalErrorResponse($"Failed to end group call: {ex.Message}");
        }
    }

    // PUT /api/groupcalls/{callId}/media
    [HttpPut("{callId}/media")]
    public async Task<IActionResult> ToggleMedia(string callId, [FromBody] ToggleMediaRequest request)
    {
        try
        {
            // Get user ID from JWT token
            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return BadRequestResponse("User not authenticated");
            }

            var result = await _groupCallService.ToggleMediaAsync(callId, userId.Value, request);

            if (!result.IsSuccess)
            {
                return BadRequestResponse(result.ErrorMessage ?? "Unknown error");
            }

            return OkResponse($"Media {request.media_type} {(request.enabled ? "enabled" : "disabled")} successfully");
        }
        catch (Exception ex)
        {
            return InternalErrorResponse($"Failed to toggle media: {ex.Message}");
        }
    }

    // GET /api/groupcalls/history/{groupId}?page=1&limit=20
    [HttpGet("history/{groupId}")]
    public async Task<IActionResult> GetGroupCallHistory(int groupId, int page = 1, int limit = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (limit < 1 || limit > 100) limit = 20;

            var result = await _groupCallService.GetGroupCallHistoryAsync(groupId, page, limit);

            if (!result.IsSuccess)
            {
                return BadRequestResponse(result.ErrorMessage ?? "Unknown error");
            }

            return OkResponse(result.Data, "Group call history retrieved successfully");
        }
        catch (Exception ex)
        {
            return InternalErrorResponse($"Failed to get group call history: {ex.Message}");
        }
    }

    // PUT /api/groupcalls/{callId}/connection-quality
    [HttpPut("{callId}/connection-quality")]
    public async Task<IActionResult> UpdateConnectionQuality(string callId, [FromBody] UpdateConnectionQualityRequest request)
    {
        try
        {
            // Get user ID from JWT token
            var userId = GetAuthenticatedUserId();
            if (userId == null)
            {
                return BadRequestResponse("User not authenticated");
            }

            var result = await _groupCallService.UpdateConnectionQualityAsync(callId, userId.Value, request.quality);

            if (!result.IsSuccess)
            {
                return BadRequestResponse(result.ErrorMessage ?? "Unknown error");
            }

            return OkResponse("Connection quality updated successfully");
        }
        catch (Exception ex)
        {
            return InternalErrorResponse($"Failed to update connection quality: {ex.Message}");
        }
    }
}

// Additional DTO for connection quality update
public class UpdateConnectionQualityRequest
{
    public string quality { get; set; } = "good"; // "excellent", "good", "poor", "disconnected"
}