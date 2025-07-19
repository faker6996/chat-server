namespace ChatServer.Models;

public class GroupInfoResponse
{
    public int id { get; set; }

    public string name { get; set; } = string.Empty;

    public string? description { get; set; }

    public string? avatar_url { get; set; }

    public int member_count { get; set; }

    public int max_members { get; set; }

    public bool is_public { get; set; }

    public bool require_approval { get; set; }

    public string? invite_link { get; set; }

    public DateTime created_at { get; set; }

    public UserResponse? created_by { get; set; }

    public string? user_role { get; set; } // Current user's role in the group

    public int online_count { get; set; }

    public List<GroupMemberResponse>? recent_members { get; set; }
}