
using ChatServer.Constants;

namespace ChatServer.Models
{ 
    public class SendMessageRequest
    {
        // Ai là người gửi? Trong thực tế, bạn sẽ lấy thông tin này từ JWT token sau khi xác thực.
        public required string SenderId { get; set; }

        public required string Content { get; set; }

        // Loại tin nhắn là gì?
        public required MESSAGE_TYPE MessageType { get; set; }

        // Đích đến của tin nhắn
        // - Nếu là Private, đây là UserId của người nhận.
        // - Nếu là Group, đây là GroupId.
        // - Nếu là Public, có thể để trống.
        public string? TargetId { get; set; }
    }
}