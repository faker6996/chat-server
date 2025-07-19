using ChatServer.Core.Constants;

namespace ChatServer.Core.Models
{
    public class MessageResponse
    {
        public int id { get; set; }
        public int conversation_id { get; set; }
        public int sender_id { get; set; }
        public required string content { get; set; }
        public MESSAGE_TYPE message_type { get; set; }
        public string content_type { get; set; } = "text";
        public DateTime created_at { get; set; }
        public required string status { get; set; }
        public int? target_id { get; set; }
        public int? reply_to_message_id { get; set; }
        public List<Attachment>? attachments { get; set; }
        public List<MessageReaction>? reactions { get; set; }
        public MessageResponse? replied_message { get; set; }
    }
}