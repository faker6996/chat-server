namespace ChatServer.Core.Models;

public class ToggleMediaRequest
{
    public string media_type { get; set; } = string.Empty; // "audio" or "video"
    
    public bool enabled { get; set; }
}