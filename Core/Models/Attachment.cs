using ChatServer.Infrastructure.Repositories.Attributes;

namespace ChatServer.Core.Models
{
    [Table("attachments")]
    public class Attachment
    {
        [Key]
        public long id { get; set; }
        
        public int message_id { get; set; }
        
        public required string file_name { get; set; }
        
        public required string file_url { get; set; }
        
        public required string file_type { get; set; }
        
        public long file_size { get; set; }
        
        public DateTime created_at { get; set; }
    }
}