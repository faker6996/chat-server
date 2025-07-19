using ChatServer.Infrastructure.Repositories.Attributes;

namespace ChatServer.Core.Models;

[Table("conversation_participants")]
public class ConversationParticipant
{
    [Key]
    public int id { get; set; }

    public int conversation_id { get; set; }

    public int user_id { get; set; }

    public DateTime joined_at { get; set; } = DateTime.UtcNow;

    public DateTime? last_seen_at { get; set; }

    public string role { get; set; } = "member"; // admin, moderator, member

    // Navigation properties (not mapped)
    [NotMapped]
    public User? user { get; set; }

    [NotMapped]
    public Conversation? conversation { get; set; }
}