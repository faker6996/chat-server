namespace ChatServer.Core.Models;

public class GroupStatsQueryResult
{
    public int total_members { get; set; }
    public int online_members { get; set; }
    public int admin_count { get; set; }
    public int moderator_count { get; set; }
}