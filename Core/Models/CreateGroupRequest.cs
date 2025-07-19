namespace ChatServer.Core.Models;

public class CreateGroupRequest
{
    public string name { get; set; } = string.Empty;

    public string? description { get; set; }

    public string? avatar_url { get; set; }

    public List<int> initial_members { get; set; } = new();

    public bool is_public { get; set; } = false;

    public bool require_approval { get; set; } = false;

    public int max_members { get; set; } = 256;
}