using ChatServer.Infrastructure.Repositories.Attributes;

namespace ChatServer.Core.Models
{
    [Table("message_reactions")]
    public class MessageReaction
    {
        [Key]
        public int id { get; set; }
        
        public int message_id { get; set; }
        
        public int user_id { get; set; }
        
        public required string emoji { get; set; }
        
        public DateTime reacted_at { get; set; }
    }
}