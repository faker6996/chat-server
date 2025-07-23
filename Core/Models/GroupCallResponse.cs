namespace ChatServer.Core.Models;

public class GroupCallResponse
{
    public string id { get; set; } = string.Empty;
    
    public int group_id { get; set; }
    
    public string? group_name { get; set; }
    
    public int initiator_id { get; set; }
    
    public string? initiator_name { get; set; }
    
    public string call_type { get; set; } = string.Empty;
    
    public DateTime started_at { get; set; }
    
    public DateTime? ended_at { get; set; }
    
    public string status { get; set; } = string.Empty;
    
    public int max_participants { get; set; }
    
    public int active_participants_count { get; set; }
    
    public List<CallParticipantResponse> participants { get; set; } = new();
}