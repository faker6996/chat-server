namespace ChatServer.Core.Models;

public class UpdateGroupRequest
{
    public string? name { get; set; }

    public string? description { get; set; }

    public string? avatar_url { get; set; }

    public bool? is_public { get; set; }

    public bool? require_approval { get; set; }

    public int? max_members { get; set; }
}