namespace ChatServer.Core.Models;

public class StartGroupCallRequest
{
    public string call_type { get; set; } = "video"; // "video" or "audio"
    
    public List<int>? invite_user_ids { get; set; } // Optional: specific users to invite
    
    public int? max_participants { get; set; } = 10; // Optional: override default max participants
}