
using ChatServer.Constants;

namespace ChatServer.Models
{
    public class ChatMessage
    {
        /// <summary>
        /// ID của người gửi.
        /// </summary>
        public required int sender_id { get; set; }

        /// <summary>
        /// Nội dung tin nhắn.
        /// </summary>
        public required string content { get; set; }

        /// <summary>
        /// Thời gian gửi tin (UTC).
        /// </summary>
        public DateTime timestamp { get; set; }


        /// <summary>
        /// Loại tin nhắn (Private, Group, Public).
        /// </summary>
        public MESSAGE_TYPE message_type { get; set; }

        /// <summary>
        /// Đích đến của tin nhắn (UserId hoặc GroupId).
        /// </summary>
        public int? target_id { get; set; }

        /// <summary>
        /// Tên hiển thị của người gửi (nếu có).
        /// </summary>
        public string? display_name { get; set; }

        public DateTime created_at { get; set; }

        public MessageStatus status { get; set; }
    }

    public enum MessageStatus { Sending, Sent, Delivered, Read, Failed }
}