namespace ChatServer.Core.Models;

public class AddMembersRequest
{
    public List<int> user_ids { get; set; } = new();
}