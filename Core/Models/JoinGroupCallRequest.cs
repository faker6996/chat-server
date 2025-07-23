namespace ChatServer.Core.Models;

public class JoinGroupCallRequest
{
    public string? offer_data { get; set; } // WebRTC offer
    
    public bool is_audio_enabled { get; set; } = true;
    
    public bool is_video_enabled { get; set; } = true;
}