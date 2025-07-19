
using ChatServer.Core.Constants;
using ChatServer.Infrastructure.Repositories.Attributes;

namespace ChatServer.Core.Models
{

    [Table("messages")]
    public class Message
    {
        /// <summary>
        /// ID chính của tin nhắn trong database (Primary Key).
        /// </summary>
        [Key]
        public int id { get; set; }

        /// <summary>
        /// ID của cuộc hội thoại, giúp nhóm các tin nhắn lại.
        /// </summary>
        public int conversation_id { get; set; }

        /// <summary>
        /// ID của người gửi.
        /// </summary>
        public int sender_id { get; set; }



        /// <summary>
        /// Nội dung tin nhắn (text, URL hình ảnh, URL file).
        /// </summary>
        public required string content { get; set; }

        /// <summary>
        /// Loại tin nhắn (PUBLIC/PRIVATE/GROUP).
        /// </summary>
        public MESSAGE_TYPE message_type { get; set; }

        /// <summary>
        /// Loại nội dung (text/image/file).
        /// </summary>
        public string content_type { get; set; } = "text";

        /// <summary>
        /// Thời gian tin nhắn được tạo (UTC).
        /// </summary>
        public DateTime created_at { get; set; }

        public required string status { get; set; }

        /// <summary>
        /// ID của tin nhắn được reply (nếu có).
        /// </summary>
        public int? reply_to_message_id { get; set; }

        /// <summary>
        /// ID của người nhận hoặc nhóm nhận. Có thể null.
        /// </summary>
        [NotMapped]
        public int? target_id { get; set; }
    }

    public enum MessageStatus { Sending, Sent, Delivered, Read, Failed }
}