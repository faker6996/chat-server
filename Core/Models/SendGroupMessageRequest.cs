namespace ChatServer.Core.Models;

public class SendGroupMessageRequest
{
    public int group_id { get; set; }

    public string content { get; set; } = string.Empty;

    public string content_type { get; set; } = "text"; // text, image, file

    public int? reply_to_message_id { get; set; }

    public List<AttachmentRequest>? attachments { get; set; }
}