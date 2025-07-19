using System.Threading.Tasks;

namespace ChatServer.Infrastructure.Repositories.Messenger
{
    /// <summary>
    /// Interface định nghĩa các phương thức cho kho lưu trữ người dùng.
    /// </summary>
    public interface IUserRepo
    {
        /// <summary>
        /// Cập nhật trạng thái online/offline và thời gian hoạt động cuối cùng của người dùng.
        /// </summary>
        /// <param name="userId">ID của người dùng cần cập nhật.</param>
        /// <param name="isActive">Trạng thái mới (true = online, false = offline).</param>
        Task UpdateUserStatusAsync(int userId, bool isActive);
    }
}
