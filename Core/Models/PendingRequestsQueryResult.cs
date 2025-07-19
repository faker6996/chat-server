namespace ChatServer.Core.Models;

public class PendingRequestsQueryResult
{
    public int id { get; set; }
    public string? message { get; set; }
    public string status { get; set; } = string.Empty;
    public DateTime requested_at { get; set; }
    public int? reviewed_by { get; set; }
    public DateTime? reviewed_at { get; set; }
    public string? reason { get; set; }
    public int user_id { get; set; }
    public string user_name { get; set; } = string.Empty;
    public string user_email { get; set; } = string.Empty;
    public string? user_avatar { get; set; }
}