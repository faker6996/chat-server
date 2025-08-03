using ChatServer.Controllers;
using ChatServer.Core.Models;
using ChatServer.Presentation.SignalR.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ChatServer.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SessionController : BaseApiController
    {
        private readonly IHubContext<ChatHub, IChatHub> _hubContext;
        private readonly ILogger<SessionController> _logger;

        public SessionController(IHubContext<ChatHub, IChatHub> hubContext, ILogger<SessionController> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        [HttpPost("notify-session-invalidated")]
        public async Task<IActionResult> NotifySessionInvalidated([FromBody] SessionInvalidationRequest request)
        {
            _logger.LogInformation("🔔 FE called notify-session-invalidated API for user {UserId} with reason: {Reason}, message: {Message}", 
                request.UserId, request.Reason, request.Message);

            try
            {
                await _hubContext.Clients.User(request.UserId.ToString())
                    .SessionInvalidated(new {
                        reason = request.Reason,
                        message = request.Message,
                        timestamp = DateTimeOffset.UtcNow
                    });

                _logger.LogInformation("✅ Session invalidation notification sent successfully to user {UserId} via SignalR", 
                    request.UserId);

                return OkResponse("Session invalidation notification sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error sending session invalidation notification to user {UserId}", request.UserId);
                return InternalErrorResponse("Failed to send session invalidation notification");
            }
        }
    }
}