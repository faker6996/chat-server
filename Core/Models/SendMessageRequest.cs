
using ChatServer.Core.Constants;

namespace ChatServer.Core.Models
{
    public class SendMessageRequest
    {
        // Ai là người gửi? Trong thực tế, bạn sẽ lấy thông tin này từ JWT token sau khi xác thực.
        public required int sender_id { get; set; }

        public int? conversation_id { get; set; }
        public required string content { get; set; }

        // Loại tin nhắn là gì?
        public required MESSAGE_TYPE message_type { get; set; }
        
        // Loại nội dung (text/image/file)
        public string content_type { get; set; } = "text";

        // Đích đến của tin nhắn
        // - Nếu là Private, đây là UserId của người nhận.
        // - Nếu là Group, đây là GroupId.
        // - Nếu là Public, có thể để trống.
        // Bắt buộc khi không có conversation_id
        public int? target_id { get; set; }
        
        // Danh sách các file đính kèm
        public List<AttachmentRequest>? attachments { get; set; }
        
        // ID của tin nhắn được reply (nếu có)
        public int? reply_to_message_id { get; set; }
    }
}