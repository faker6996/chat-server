namespace ChatServer.Core.Models
{
    public class AttachmentRequest
    {
        public required string file_name { get; set; }
        
        public required string file_url { get; set; }
        
        public required string file_type { get; set; }
        
        public long file_size { get; set; }
    }
}