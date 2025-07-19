using ChatServer.Repositories.Attributes;

namespace ChatServer.Models;

[Table("group_join_requests")]
public class GroupJoinRequest
{
    [Key]
    public int id { get; set; }

    public int conversation_id { get; set; }

    public int user_id { get; set; }

    public string? message { get; set; }

    public string status { get; set; } = "pending"; // pending, approved, rejected

    public DateTime requested_at { get; set; } = DateTime.UtcNow;

    public int? reviewed_by { get; set; }

    public DateTime? reviewed_at { get; set; }

    public string? reason { get; set; }

    // Navigation properties (not mapped)
    [NotMapped]
    public User? user { get; set; }

    [NotMapped]
    public User? reviewer { get; set; }

    [NotMapped]
    public Conversation? conversation { get; set; }
}