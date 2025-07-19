// Models/ApiResponse.cs
namespace ChatServer.Core.Models
{
    // Lớp generic để bọc các response thành công có dữ liệu
    public class ApiResponse<T>
    {
        public bool success { get; set; }
        public string message { get; set; }
        public T? data { get; set; } // Dữ liệu có thể là null

        public ApiResponse(bool success, string message, T? data)
        {
            this.success = success;
            this.message = message;
            this.data = data;
        }
    }

    // Lớp không generic để bọc các response không có dữ liệu (lỗi hoặc thành công nhưng không cần data)
    // Nó chỉ đơn giản kế thừa lớp trên với kiểu object và data là null.
    public class ApiResponse : ApiResponse<object>
    {
        public ApiResponse(bool success, string message)
            : base(success, message, null)
        {
        }
    }
}