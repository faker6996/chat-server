
using ChatServer.Constants;

namespace ChatServer.Models
{
    public class ChatMessage
    {
        /// <summary>
        /// ID của người gửi.
        /// </summary>
        public required string User { get; set; }

        /// <summary>
        /// Nội dung tin nhắn.
        /// </summary>
        public required string Content { get; set; }

        /// <summary>
        /// Thời gian gửi tin (UTC).
        /// </summary>
        public DateTime Timestamp { get; set; }


        /// <summary>
        /// Loại tin nhắn (Private, Group, Public).
        /// </summary>
        public MESSAGE_TYPE MessageType { get; set; }

        /// <summary>
        /// Đích đến của tin nhắn (UserId hoặc GroupId).
        /// </summary>
        public string? TargetId { get; set; }

        /// <summary>
        /// Tên hiển thị của người gửi (nếu có).
        /// </summary>
        public string? DisplayName { get; set; }
    }
}