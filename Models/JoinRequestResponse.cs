namespace ChatServer.Models;

public class JoinRequestResponse
{
    public int id { get; set; }

    public int conversation_id { get; set; }

    public int user_id { get; set; }

    public UserResponse user { get; set; } = new();

    public string? message { get; set; }

    public DateTime requested_at { get; set; }

    public string status { get; set; } = string.Empty;

    public int? reviewed_by { get; set; }

    public UserResponse? reviewer { get; set; }

    public DateTime? reviewed_at { get; set; }

    public string? reason { get; set; }
}