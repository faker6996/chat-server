namespace ChatServer.Models;

public class GroupMemberResponse
{
    public int user_id { get; set; }

    public string name { get; set; } = string.Empty;

    public string? avatar_url { get; set; }

    public string role { get; set; } = string.Empty; // admin, moderator, member

    public DateTime joined_at { get; set; }

    public DateTime? last_seen_at { get; set; }

    public bool is_online { get; set; }

    public string? email { get; set; }

    public string? user_name { get; set; }
}