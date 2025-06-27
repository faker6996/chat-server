// Controllers/BaseApiController.cs
using ChatServer.Models;
using Microsoft.AspNetCore.Mvc;

namespace ChatServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        // ================== CÁC PHƯƠNG THỨC CHO RESPONSE THÀNH CÔNG (HTTP 200) ==================

        /// <summary>
        /// Trả về response thành công với dữ liệu.
        /// </summary>
        protected IActionResult OkResponse<T>(T data, string message = "OK")
        {
            var response = new ApiResponse<T>(true, message, data);
            return Ok(response); // Ok() sẽ trả về status 200
        }

        /// <summary>
        /// Trả về response thành công không có dữ liệu.
        /// </summary>
        protected IActionResult OkResponse(string message = "OK")
        {
            var response = new ApiResponse(true, message);
            return Ok(response);
        }

        // ================== CÁC PHƯƠNG THỨC CHO RESPONSE LỖI ==================

        /// <summary>
        /// Trả về lỗi Bad Request (400).
        /// </summary>
        protected IActionResult BadRequestResponse(string message = "Bad Request")
        {
            var response = new ApiResponse(false, message);
            return BadRequest(response); // BadRequest() sẽ trả về status 400
        }

        /// <summary>
        /// Trả về lỗi Not Found (404).
        /// </summary>
        protected IActionResult NotFoundResponse(string message = "Not Found")
        {
            var response = new ApiResponse(false, message);
            return NotFound(response); // NotFound() sẽ trả về status 404
        }

        /// <summary>
        /// Trả về lỗi chung của server (500).
        /// </summary>
        protected IActionResult InternalErrorResponse(string message = "Internal Server Error")
        {
            var response = new ApiResponse(false, message);
            // Sử dụng StatusCode để chỉ định mã lỗi 500
            return StatusCode(500, response);
        }
    }
}