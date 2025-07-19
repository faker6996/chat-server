using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging; // Thêm using cho ILogger

namespace ChatServer.Presentation.SignalR
{
    // Không cần inject ILogger ở đây vì nó không phải là service được quản lý bởi DI container theo cách thông thường
    // Logging có thể được thực hiện từ các service khác nếu cần
    public class NameUserIdProvider : IUserIdProvider
    {
        public virtual string? GetUserId(HubConnectionContext connection)
        {
            // Lấy user ID từ claim đã được middleware Authentication xử lý
            // Đây là cách làm đúng và đáng tin cậy nhất.
            // Nếu `connection.User` được xác thực, nó sẽ có claim.
            var userId = connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Dòng debug (có thể xóa sau này)
            if (userId != null)
            {
                Console.WriteLine($"NameUserIdProvider: Found user ID '{userId}' from claims.");
            }
            else
            {
                // Dòng này rất quan trọng để chẩn đoán lỗi
                Console.WriteLine("NameUserIdProvider: Could not find user ID from claims. The user is likely not authenticated.");

                // Bạn vẫn có thể kiểm tra cookie ở đây để debug
                var httpContext = connection.GetHttpContext();
                if (httpContext.Request.Cookies.TryGetValue("access_token", out var tokenFromCookie))
                {
                    Console.WriteLine($"NameUserIdProvider (Debug): Found cookie 'access_token', but authentication may have failed.");
                }
                else
                {
                    Console.WriteLine("NameUserIdProvider (Debug): Did not find cookie 'access_token'.");
                }
            }

            return userId;
        }
    }
}