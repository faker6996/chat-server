using ChatServer.Core.Models;
using ChatServer.Core.Constants;
using ChatServer.Infrastructure.Repositories.GroupCall;
using ChatServer.Infrastructure.Repositories.CallParticipant;
using ChatServer.Infrastructure.Repositories.Group;
using ChatServer.Infrastructure.Repositories.Messenger;
using ChatServer.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace ChatServer.Infrastructure.Services.GroupCall;

public class GroupCallService : IGroupCallService
{
    private readonly IGroupCallRepo _groupCallRepo;
    private readonly ICallParticipantRepo _participantRepo;
    private readonly IGroupRepository _groupRepo;
    private readonly IUserRepo _userRepo;
    private readonly IChatClientNotifier _notifier;
    private readonly ILogger<GroupCallService> _logger;

    public GroupCallService(
        IGroupCallRepo groupCallRepo,
        ICallParticipantRepo participantRepo,
        IGroupRepository groupRepo,
        IUserRepo userRepo,
        IChatClientNotifier notifier,
        ILogger<GroupCallService> logger)
    {
        _groupCallRepo = groupCallRepo;
        _participantRepo = participantRepo;
        _groupRepo = groupRepo;
        _userRepo = userRepo;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task<GroupCallServiceResult> StartGroupCallAsync(int groupId, int initiatorId, StartGroupCallRequest request)
    {
        try
        {
            // Validate user is group member
            if (!await IsUserGroupMemberAsync(groupId, initiatorId))
            {
                return GroupCallServiceResult.Failure("User is not a member of this group");
            }

            // Check if group already has active call
            if (await _groupCallRepo.HasActiveCallAsync(groupId))
            {
                return GroupCallServiceResult.Failure("Group already has an active call");
            }

            // Create new group call
            var callId = Guid.NewGuid().ToString();
            var groupCall = new Core.Models.GroupCall
            {
                id = callId,
                group_id = groupId,
                initiator_id = initiatorId,
                call_type = request.call_type,
                started_at = DateTime.UtcNow,
                status = GroupCallConstants.CallStatus.Active,
                max_participants = request.max_participants ?? GroupCallConstants.DefaultValues.MaxParticipants,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            };

            await _groupCallRepo.InsertGroupCallAsync(groupCall);

            // Auto-join initiator
            var initiatorParticipant = new Core.Models.CallParticipant
            {
                call_id = callId,
                user_id = initiatorId,
                joined_at = DateTime.UtcNow,
                is_audio_enabled = true,
                is_video_enabled = request.call_type == GroupCallConstants.CallType.Video,
                connection_quality = GroupCallConstants.DefaultValues.DefaultConnectionQuality,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            };

            await _participantRepo.InsertAsync(initiatorParticipant);

            _logger.LogInformation("Group call {CallId} started by user {UserId} in group {GroupId}", 
                callId, initiatorId, groupId);

            var response = await CreateGroupCallResponseAsync(groupCall);
            
            // Notify group members about call start
            await _notifier.GroupCallStartedAsync(groupId, response);
            
            return GroupCallServiceResult.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting group call for group {GroupId} by user {UserId}", groupId, initiatorId);
            return GroupCallServiceResult.Failure("Failed to start group call");
        }
    }

    public async Task<GroupCallServiceResult> JoinGroupCallAsync(string callId, int userId, JoinGroupCallRequest request)
    {
        try
        {
            var call = await _groupCallRepo.GetByStringIdAsync(callId);
            if (call == null)
            {
                return GroupCallServiceResult.Failure("Call not found");
            }

            if (call.status != GroupCallConstants.CallStatus.Active)
            {
                return GroupCallServiceResult.Failure("Call is not active");
            }

            // Validate user can join call
            if (!await ValidateUserCanJoinCallAsync(callId, userId))
            {
                return GroupCallServiceResult.Failure("User cannot join this call");
            }

            // Check if user is already in call
            if (await _participantRepo.IsUserInCallAsync(callId, userId))
            {
                return GroupCallServiceResult.Failure("User is already in the call");
            }

            // Check participant limit
            var currentParticipants = await _participantRepo.GetActiveParticipantsCountAsync(callId);
            if (currentParticipants >= call.max_participants)
            {
                return GroupCallServiceResult.Failure("Call has reached maximum participants");
            }

            // Add participant
            var participant = new Core.Models.CallParticipant
            {
                call_id = callId,
                user_id = userId,
                joined_at = DateTime.UtcNow,
                is_audio_enabled = request.is_audio_enabled,
                is_video_enabled = request.is_video_enabled,
                connection_quality = GroupCallConstants.DefaultValues.DefaultConnectionQuality,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            };

            await _participantRepo.InsertAsync(participant);

            _logger.LogInformation("User {UserId} joined group call {CallId}", userId, callId);

            // Create participant response for notification
            var user = await _userRepo.GetByIdAsync(userId);
            var participantResponse = new CallParticipantResponse
            {
                id = participant.id,
                user_id = participant.user_id,
                user_name = user?.user_name ?? "Unknown",
                avatar_url = user?.avatar_url,
                joined_at = participant.joined_at,
                left_at = participant.left_at,
                is_audio_enabled = participant.is_audio_enabled,
                is_video_enabled = participant.is_video_enabled,
                connection_quality = participant.connection_quality
            };

            // Notify group members about participant join
            await _notifier.GroupCallParticipantJoinedAsync(call.group_id, callId, participantResponse);

            var response = await CreateGroupCallResponseAsync(call);
            return GroupCallServiceResult.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining group call {CallId} for user {UserId}", callId, userId);
            return GroupCallServiceResult.Failure("Failed to join group call");
        }
    }

    public async Task<GroupCallServiceResult> LeaveGroupCallAsync(string callId, int userId)
    {
        try
        {
            var call = await _groupCallRepo.GetByStringIdAsync(callId);
            if (call == null)
            {
                return GroupCallServiceResult.Failure("Call not found");
            }

            if (!await _participantRepo.IsUserInCallAsync(callId, userId))
            {
                return GroupCallServiceResult.Failure("User is not in the call");
            }

            await _participantRepo.LeaveCallAsync(callId, userId);

            _logger.LogInformation("User {UserId} left group call {CallId}", userId, callId);

            // Notify group members about participant leave
            await _notifier.GroupCallParticipantLeftAsync(call.group_id, callId, userId, "User left");

            // Check if call should be ended (no active participants)
            var activeParticipants = await _participantRepo.GetActiveParticipantsCountAsync(callId);
            if (activeParticipants == 0)
            {
                await _groupCallRepo.UpdateGroupCallStatusAsync(callId, GroupCallConstants.CallStatus.Ended, DateTime.UtcNow);
                _logger.LogInformation("Group call {CallId} ended - no active participants", callId);
                
                // Notify group members about call end
                await _notifier.GroupCallEndedAsync(call.group_id, callId, "No active participants");
            }

            return GroupCallServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving group call {CallId} for user {UserId}", callId, userId);
            return GroupCallServiceResult.Failure("Failed to leave group call");
        }
    }

    public async Task<GroupCallServiceResult> EndGroupCallAsync(string callId, int userId)
    {
        try
        {
            var call = await _groupCallRepo.GetByStringIdAsync(callId);
            if (call == null)
            {
                return GroupCallServiceResult.Failure("Call not found");
            }

            if (call.status != GroupCallConstants.CallStatus.Active)
            {
                return GroupCallServiceResult.Failure("Call is not active");
            }

            // Only initiator or group admin can end call
            if (call.initiator_id != userId)
            {
                var userRole = await _groupRepo.GetUserRoleInGroupAsync(call.group_id, userId);
                if (userRole != "admin" && userRole != "moderator")
                {
                    return GroupCallServiceResult.Failure("Only call initiator or group admins can end the call");
                }
            }

            await _groupCallRepo.UpdateGroupCallStatusAsync(callId, GroupCallConstants.CallStatus.Ended, DateTime.UtcNow);

            // Mark all active participants as left
            var activeParticipants = await _participantRepo.GetActiveParticipantsAsync(callId);
            foreach (var participant in activeParticipants)
            {
                await _participantRepo.LeaveCallAsync(callId, participant.user_id);
            }

            _logger.LogInformation("Group call {CallId} ended by user {UserId}", callId, userId);

            // Get user name for notification
            var endingUser = await _userRepo.GetByIdAsync(userId);
            var endingUserName = endingUser?.user_name ?? "Unknown";
            
            // Notify group members about call end
            await _notifier.GroupCallEndedAsync(call.group_id, callId, $"Ended by {endingUserName}");

            return GroupCallServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending group call {CallId} by user {UserId}", callId, userId);
            return GroupCallServiceResult.Failure("Failed to end group call");
        }
    }

    public async Task<GroupCallServiceResult> GetActiveGroupCallAsync(int groupId)
    {
        try
        {
            var call = await _groupCallRepo.GetActiveGroupCallAsync(groupId);
            if (call == null)
            {
                return GroupCallServiceResult.Success(null);
            }

            var response = await CreateGroupCallResponseAsync(call);
            return GroupCallServiceResult.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active group call for group {GroupId}", groupId);
            return GroupCallServiceResult.Failure("Failed to get active group call");
        }
    }

    public async Task<GroupCallServiceResult> GetCallParticipantsAsync(string callId)
    {
        try
        {
            var participants = await _participantRepo.GetAllParticipantsAsync(callId);
            var participantResponses = new List<CallParticipantResponse>();

            foreach (var participant in participants)
            {
                var user = await _userRepo.GetByIdAsync(participant.user_id);
                participantResponses.Add(new CallParticipantResponse
                {
                    id = participant.id,
                    user_id = participant.user_id,
                    user_name = user?.user_name ?? "Unknown",
                    avatar_url = user?.avatar_url,
                    joined_at = participant.joined_at,
                    left_at = participant.left_at,
                    is_audio_enabled = participant.is_audio_enabled,
                    is_video_enabled = participant.is_video_enabled,
                    connection_quality = participant.connection_quality
                });
            }

            return GroupCallServiceResult.Success(participantResponses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting participants for call {CallId}", callId);
            return GroupCallServiceResult.Failure("Failed to get call participants");
        }
    }

    public async Task<GroupCallServiceResult> ToggleMediaAsync(string callId, int userId, ToggleMediaRequest request)
    {
        try
        {
            if (!await _participantRepo.IsUserInCallAsync(callId, userId))
            {
                return GroupCallServiceResult.Failure("User is not in the call");
            }

            var success = await _participantRepo.ToggleMediaAsync(callId, userId, request.media_type, request.enabled);
            if (!success)
            {
                return GroupCallServiceResult.Failure("Failed to toggle media");
            }

            _logger.LogInformation("User {UserId} toggled {MediaType} to {Enabled} in call {CallId}", 
                userId, request.media_type, request.enabled, callId);

            // Get call info for group ID
            var call = await _groupCallRepo.GetByStringIdAsync(callId);
            if (call != null)
            {
                // Notify group members about media toggle
                await _notifier.GroupCallMediaToggledAsync(call.group_id, callId, userId, request.media_type, request.enabled);
            }

            return GroupCallServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling media for user {UserId} in call {CallId}", userId, callId);
            return GroupCallServiceResult.Failure("Failed to toggle media");
        }
    }

    public async Task<GroupCallServiceResult> UpdateConnectionQualityAsync(string callId, int userId, string quality)
    {
        try
        {
            var success = await _participantRepo.UpdateConnectionQualityAsync(callId, userId, quality);
            if (!success)
            {
                return GroupCallServiceResult.Failure("Failed to update connection quality");
            }

            return GroupCallServiceResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating connection quality for user {UserId} in call {CallId}", userId, callId);
            return GroupCallServiceResult.Failure("Failed to update connection quality");
        }
    }

    public async Task<GroupCallServiceResult> GetGroupCallHistoryAsync(int groupId, int page = 1, int limit = 20)
    {
        try
        {
            var offset = (page - 1) * limit;
            var calls = await _groupCallRepo.GetGroupCallHistoryAsync(groupId, offset, limit);
            
            var responses = new List<GroupCallResponse>();
            foreach (var call in calls)
            {
                responses.Add(await CreateGroupCallResponseAsync(call));
            }

            return GroupCallServiceResult.Success(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting call history for group {GroupId}", groupId);
            return GroupCallServiceResult.Failure("Failed to get call history");
        }
    }

    public async Task<bool> ValidateUserCanJoinCallAsync(string callId, int userId)
    {
        try
        {
            var call = await _groupCallRepo.GetByStringIdAsync(callId);
            if (call == null) return false;

            return await IsUserGroupMemberAsync(call.group_id, userId);
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsUserGroupMemberAsync(int groupId, int userId)
    {
        try
        {
            return await _groupRepo.IsUserInGroupAsync(groupId, userId);
        }
        catch
        {
            return false;
        }
    }

    private async Task<GroupCallResponse> CreateGroupCallResponseAsync(Core.Models.GroupCall call)
    {
        var group = await _groupRepo.GetGroupByIdAsync(call.group_id);
        var initiator = await _userRepo.GetByIdAsync(call.initiator_id);
        var participants = await _participantRepo.GetAllParticipantsAsync(call.id);
        var activeCount = await _participantRepo.GetActiveParticipantsCountAsync(call.id);

        var participantResponses = new List<CallParticipantResponse>();
        foreach (var participant in participants)
        {
            var user = await _userRepo.GetByIdAsync(participant.user_id);
            participantResponses.Add(new CallParticipantResponse
            {
                id = participant.id,
                user_id = participant.user_id,
                user_name = user?.user_name ?? "Unknown",
                avatar_url = user?.avatar_url,
                joined_at = participant.joined_at,
                left_at = participant.left_at,
                is_audio_enabled = participant.is_audio_enabled,
                is_video_enabled = participant.is_video_enabled,
                connection_quality = participant.connection_quality
            });
        }

        return new GroupCallResponse
        {
            id = call.id,
            group_id = call.group_id,
            group_name = group?.name ?? "Unknown",
            initiator_id = call.initiator_id,
            initiator_name = initiator?.user_name ?? "Unknown",
            call_type = call.call_type,
            started_at = call.started_at,
            ended_at = call.ended_at,
            status = call.status,
            max_participants = call.max_participants,
            active_participants_count = activeCount,
            participants = participantResponses
        };
    }
}