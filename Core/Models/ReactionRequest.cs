namespace ChatServer.Core.Models
{
    public class ReactionRequest
    {
        public required int message_id { get; set; }
        
        public required int user_id { get; set; }
        
        public required string emoji { get; set; }
    }
}