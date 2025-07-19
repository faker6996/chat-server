namespace ChatServer.Core.Models;

public enum GroupEventType
{
    GroupCreated,
    GroupUpdated,
    GroupDeleted,
    MemberAdded,
    MemberRemoved,
    MemberPromoted,
    GroupMessage,
    JoinRequest,
    RequestHandled,
    UserJoinedGroup,
    UserLeftGroup,
    InviteLinkGenerated
}

public class GroupEventMessage
{
    public GroupEventType type { get; set; }
    
    public int group_id { get; set; }
    
    public int? user_id { get; set; }
    
    public string data { get; set; } = string.Empty; // JSON payload
    
    public DateTime timestamp { get; set; } = DateTime.UtcNow;
    
    public string? routing_key { get; set; }
}