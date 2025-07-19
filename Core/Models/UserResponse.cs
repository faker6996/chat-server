namespace ChatServer.Core.Models;

public class UserResponse
{
    public int id { get; set; }

    public string name { get; set; } = string.Empty;

    public string? user_name { get; set; }

    public string? email { get; set; }

    public string? avatar_url { get; set; }

    public bool is_active { get; set; }

    public DateTime? last_seen { get; set; }

    public bool is_online => last_seen.HasValue && last_seen.Value > DateTime.UtcNow.AddMinutes(-5);
}