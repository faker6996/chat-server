using ChatServer.Infrastructure.Repositories.Attributes;

namespace ChatServer.Core.Models;

[Table("call_participants")]
public class CallParticipant
{
    [Key]
    public int id { get; set; }
    
    public string call_id { get; set; } = string.Empty;
    
    public int user_id { get; set; }
    
    public DateTime joined_at { get; set; }
    
    public DateTime? left_at { get; set; }
    
    public bool is_audio_enabled { get; set; } = true;
    
    public bool is_video_enabled { get; set; } = true;
    
    public string connection_quality { get; set; } = "good"; // "excellent", "good", "poor", "disconnected"
    
    public DateTime created_at { get; set; }
    
    public DateTime updated_at { get; set; }
}