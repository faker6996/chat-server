using ChatServer.Infrastructure.Repositories.Attributes;

namespace ChatServer.Core.Models;

[Table("group_calls")]
public class GroupCall
{
    [Key]
    public string id { get; set; } = string.Empty;
    
    public int group_id { get; set; }
    
    public int initiator_id { get; set; }
    
    public string call_type { get; set; } = string.Empty; // "audio" or "video"
    
    public DateTime started_at { get; set; }
    
    public DateTime? ended_at { get; set; }
    
    public string status { get; set; } = "active"; // "active" or "ended"
    
    public int max_participants { get; set; } = 10;
    
    public DateTime created_at { get; set; }
    
    public DateTime updated_at { get; set; }
}