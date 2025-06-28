
using ChatServer.Constants;
using ChatServer.Repositories.Attributes;

namespace ChatServer.Models
{

    [Table("messages")]
    public class Message
    {
        /// <summary>
        /// ID chính của tin nhắn trong database (Primary Key).
        /// </summary>
        [Key]
        public long id { get; set; }

        /// <summary>
        /// ID của cuộc hội thoại, giúp nhóm các tin nhắn lại.
        /// </summary>
        public string? conversation_id { get; set; }

        /// <summary>
        /// ID của người gửi.
        /// </summary>
        public int sender_id { get; set; }

        /// <summary>
        /// ID của người nhận hoặc nhóm nhận. Có thể null.
        /// </summary>
        public int? target_id { get; set; }

        /// <summary>
        /// Nội dung tin nhắn (text, URL hình ảnh, URL file).
        /// </summary>
        public required string content { get; set; }

        /// <summary>
        /// Loại nội dung của tin nhắn.
        /// </summary>
        public MESSAGE_TYPE message_type { get; set; }

        /// <summary>
        /// Thời gian tin nhắn được tạo (UTC).
        /// </summary>
        public DateTime created_at { get; set; }

        /// <summary>
        /// Trạng thái của tin nhắn (gửi, đã nhận, đã xem...).
        /// </summary>
        public MessageStatus status { get; set; }

        [NotMapped] // 👈 Thuộc tính này sẽ được bỏ qua khi INSERT/UPDATE
        public string? ExtraInfo { get; set; }
    }

    public enum MessageStatus { Sending, Sent, Delivered, Read, Failed }
}