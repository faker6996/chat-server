using ChatServer.Infrastructure.Repositories.Attributes;

namespace ChatServer.Core.Models;

[Table("conversations")]
public class Conversation
{
    [Key]
    public int id { get; set; }

    public bool is_group { get; set; } = false;

    public string? name { get; set; }

    public string? description { get; set; }

    public string? avatar_url { get; set; }

    public int max_members { get; set; } = 256;

    public bool is_public { get; set; } = false;

    public string? invite_link { get; set; }

    public bool require_approval { get; set; } = false;

    public int? created_by { get; set; }

    public DateTime created_at { get; set; } = DateTime.UtcNow;

    // Navigation properties (not mapped)
    [NotMapped]
    public List<ConversationParticipant>? participants { get; set; }

    [NotMapped]
    public User? creator { get; set; }

    [NotMapped]
    public int member_count { get; set; }

    [NotMapped]
    public int online_count { get; set; }
}