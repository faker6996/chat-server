using ChatServer.Repositories.Attributes;

namespace ChatServer.Models
{
    [Table("attachments")]
    public class Attachment
    {
        [Key]
        public long id { get; set; }
        
        public long message_id { get; set; }
        
        public required string file_name { get; set; }
        
        public required string file_url { get; set; }
        
        public required string file_type { get; set; }
        
        public long file_size { get; set; }
        
        public DateTime created_at { get; set; }
    }
}