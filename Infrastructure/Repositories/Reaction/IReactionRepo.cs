using ChatServer.Core.Models;
using ChatServer.Infrastructure.Repositories.Base;

namespace ChatServer.Infrastructure.Repositories.Reaction
{
    public interface IReactionRepo : IBaseRepository<MessageReaction>
    {
        Task<MessageReaction?> GetByMessageUserEmojiAsync(int messageId, int userId, string emoji);
        Task<IEnumerable<MessageReaction>> GetByMessageIdAsync(int messageId);
        Task<MessageReaction> AddReactionAsync(int messageId, int userId, string emoji);
        Task<bool> RemoveReactionAsync(int messageId, int userId, string emoji);
    }
}