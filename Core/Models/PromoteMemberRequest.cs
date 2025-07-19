namespace ChatServer.Models;

public class PromoteMemberRequest
{
    public int user_id { get; set; }

    public string role { get; set; } = string.Empty; // admin, moderator, member
}