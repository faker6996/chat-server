// File: Application/IMessageService.cs
using ChatServer.Models;

namespace ChatServer.Applications;

// Định nghĩa một DTO (Data Transfer Object) để chứa kết quả trả về từ service
public record MessageServiceResult(bool IsSuccess, string? ErrorMessage = null, object? Data = null);

public interface IMessageService
{
    /// <summary>
    /// Xử lý logic gửi tin nhắn và đẩy vào message broker.
    /// </summary>
    /// <param name="request">Dữ liệu đầu vào từ controller.</param>
    /// <returns>Kết quả xử lý nghiệp vụ.</returns>
    Task<MessageServiceResult> SendMessageAsync(SendMessageRequest request);
}