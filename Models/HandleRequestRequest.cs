namespace ChatServer.Models;

public class HandleRequestRequest
{
    public string action { get; set; } = string.Empty; // approve, reject

    public string? reason { get; set; }
}