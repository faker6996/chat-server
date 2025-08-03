namespace ChatServer.Core.Models
{
    public class SessionInvalidationRequest
    {
        public int UserId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}