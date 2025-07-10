using ChatServer.Models;
using ChatServer.Repositories.Base;

namespace ChatServer.Repositories.Reaction
{
    public interface IReactionRepo : IBaseRepository<MessageReaction>
    {
        Task<MessageReaction?> GetByMessageUserEmojiAsync(int messageId, int userId, string emoji);
        Task<IEnumerable<MessageReaction>> GetByMessageIdAsync(int messageId);
        Task<bool> RemoveReactionAsync(int messageId, int userId, string emoji);
    }
}