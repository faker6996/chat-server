using Dapper;
using System;
using System.Data;
using System.Threading.Tasks;
using ChatServer.Models;
using ChatServer.Repositories.Base;

namespace ChatServer.Repositories.Messenger
{
    public class UserRepo : BaseRepository<User>, IUserRepo
    {
        public UserRepo(IDbConnection dbConnection) : base(dbConnection)
        {
            // Constructor không thay đổi
        }

        /// <summary>
        /// Cập nhật trạng thái online/offline của người dùng.
        /// Chỉ cập nhật last_seen khi người dùng chuyển sang offline.
        /// </summary>
        /// <param name="userId">ID của người dùng.</param>
        /// <param name="isActive">Trạng thái mới.</param>
        public async Task UpdateUserStatusAsync(int userId, bool isActive)
        {
            if (isActive)
            {
                // Khi người dùng online, chỉ cần cập nhật trạng thái is_active
                await UpdatePartialAsync(userId, new { IsActive = isActive });
            }
            else
            {
                // Khi người dùng offline, cập nhật cả is_active và last_seen
                await UpdatePartialAsync(userId, new
                {
                    IsActive = isActive,
                    LastSeen = DateTime.UtcNow // Ghi lại thời điểm cuối cùng hoạt động
                });
            }
        }
    }
}
