using ChatServer.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChatServer.Controllers
{
    public class UploadController : BaseApiController
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<UploadController> _logger;

        public UploadController(IWebHostEnvironment environment, ILogger<UploadController> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequestResponse("No file uploaded.");
                }

                // Kiểm tra kích thước file (giới hạn 10MB)
                if (file.Length > 10 * 1024 * 1024)
                {
                    return BadRequestResponse("File size exceeds 10MB limit.");
                }

                // Tạo thư mục uploads nếu chưa tồn tại
                var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Tạo tên file unique
                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Lưu file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Tạo URL cho file
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var fileUrl = $"{baseUrl}/uploads/{fileName}";

                var response = new UploadResponse
                {
                    file_url = fileUrl,
                    file_name = file.FileName,
                    file_type = file.ContentType ?? "application/octet-stream",
                    file_size = file.Length
                };

                return OkResponse(response, "File uploaded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file: {Message}", ex.Message);
                return InternalErrorResponse("An error occurred while uploading the file.");
            }
        }
    }
}