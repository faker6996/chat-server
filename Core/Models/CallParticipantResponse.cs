namespace ChatServer.Core.Models;

public class CallParticipantResponse
{
    public int id { get; set; }
    
    public int user_id { get; set; }
    
    public string? user_name { get; set; }
    
    public string? avatar_url { get; set; }
    
    public DateTime joined_at { get; set; }
    
    public DateTime? left_at { get; set; }
    
    public bool is_audio_enabled { get; set; }
    
    public bool is_video_enabled { get; set; }
    
    public string connection_quality { get; set; } = "good";
    
    public bool is_active => left_at == null;
}