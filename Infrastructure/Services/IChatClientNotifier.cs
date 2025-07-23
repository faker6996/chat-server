// File: Application/IChatClientNotifier.cs
using ChatServer.Core.Models;

namespace ChatServer.Infrastructure.Services;

// Interface định nghĩa khả năng thông báo cho client
public interface IChatClientNotifier
{
    // Existing message methods
    Task SendPublicMessageAsync(Message message);
    Task SendPrivateMessageAsync(string userId, Message message);
    Task SendGroupMessageAsync(string groupName, Message message);
    Task SendReactionAsync(int messageId, MessageReaction reaction);
    Task RemoveReactionAsync(int messageId, int userId, string emoji);

    // Group management events
    Task GroupCreatedAsync(GroupInfoResponse group);
    Task GroupUpdatedAsync(GroupInfoResponse group);
    Task GroupDeletedAsync(int groupId, string reason);
    
    // Group member events
    Task GroupMemberAddedAsync(int groupId, GroupMemberResponse member);
    Task GroupMemberRemovedAsync(int groupId, int userId, string reason);
    Task GroupMemberPromotedAsync(int groupId, int userId, string newRole);
    Task GroupMemberOnlineAsync(int groupId, int userId);
    Task GroupMemberOfflineAsync(int groupId, int userId);
    
    // Group message events  
    Task GroupMessageAsync(int groupId, MessageResponse message);
    
    // Join request events
    Task GroupJoinRequestAsync(int groupId, JoinRequestResponse request);
    Task GroupJoinRequestHandledAsync(int requestId, string action, string? reason);
    
    // Group presence events
    Task UserJoinedGroupAsync(int groupId, int userId);
    Task UserLeftGroupAsync(int groupId, int userId);
    
    // Group invite events
    Task GroupInviteLinkGeneratedAsync(int groupId, string inviteLink);
    
    // Group call events
    Task GroupCallStartedAsync(int groupId, GroupCallResponse call);
    Task GroupCallEndedAsync(int groupId, string callId, string reason);
    Task GroupCallParticipantJoinedAsync(int groupId, string callId, CallParticipantResponse participant);
    Task GroupCallParticipantLeftAsync(int groupId, string callId, int userId, string reason);
    Task GroupCallMediaToggledAsync(int groupId, string callId, int userId, string mediaType, bool enabled);
    
    // WebRTC signaling events for group calls
    Task ReceiveGroupCallOfferAsync(string callId, string fromUserId, string offerData);
    Task ReceiveGroupCallAnswerAsync(string callId, string fromUserId, string answerData);
    Task ReceiveGroupIceCandidateAsync(string callId, string fromUserId, string candidateData);
}