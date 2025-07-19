// File: Controllers/MessagesController.cs

using ChatServer.Infrastructure.Services;
using ChatServer.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChatServer.Controllers;

public class MessagesController : BaseApiController
{
    // Controller giờ chỉ phụ thuộc vào IMessageService
    private readonly IMessageService _messageService;

    // Xóa IConnection, ILogger và tiêm IMessageService vào
    public MessagesController(IMessageService messageService)
    {
        _messageService = messageService;
    }

    [HttpPost]
    public async Task<IActionResult> PostMessage([FromBody] SendMessageRequest req)
    {
        // Chỉ còn một dòng gọi đến lớp Application
        var result = await _messageService.SendMessageAsync(req);

        // Dựa vào kết quả từ service để trả về Response
        if (!result.IsSuccess)
        {
            // Nếu lỗi là do validate, trả về BadRequest. Nếu là lỗi hệ thống, trả về 500.
            // (Có thể mở rộng MessageServiceResult để chứa cả mã lỗi)
            if (result.ErrorMessage != null && result.ErrorMessage.Contains("TargetId"))
            {
                return BadRequestResponse(result.ErrorMessage);
            }
            return InternalErrorResponse(result.ErrorMessage ?? "An unknown error occurred.");
        }

        return OkResponse(result.data, "Message published successfully.");
    }

    [HttpGet("sync")]
    public async Task<IActionResult> SyncMessages([FromQuery] int conversationId, [FromQuery] int lastMessageId)
    {
        // Bạn có thể thêm logic kiểm tra xem người dùng hiện tại có thuộc về conversationId này không để tăng bảo mật

        var missedMessages = await _messageService.GetSyncMessages(conversationId, lastMessageId);

        // Trả về danh sách các tin nhắn đã lỡ
        return Ok(missedMessages);
    }
}