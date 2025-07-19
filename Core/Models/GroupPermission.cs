using ChatServer.Infrastructure.Repositories.Attributes;

namespace ChatServer.Core.Models;

[Table("group_permissions")]
public class GroupPermission
{
    [Key]
    public int id { get; set; }

    public int conversation_id { get; set; }

    public string permission_type { get; set; } = string.Empty; // add_members, remove_members, edit_info, delete_messages, pin_messages, manage_requests, promote_members, manage_invites

    public string required_role { get; set; } = "admin"; // admin, moderator, member

    public DateTime created_at { get; set; } = DateTime.UtcNow;
}