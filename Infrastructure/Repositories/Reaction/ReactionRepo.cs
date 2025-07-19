using System.Data;
using ChatServer.Models;
using ChatServer.Repositories.Base;
using Dapper;

namespace ChatServer.Infrastructure.Repositories.Reaction
{
    public class ReactionRepo : BaseRepository<MessageReaction>, IReactionRepo
    {
        public ReactionRepo(IDbConnection dbConnection) : base(dbConnection)
        {
        }

        public async Task<MessageReaction?> GetByMessageUserEmojiAsync(int messageId, int userId, string emoji)
        {
            var sql = $"SELECT * FROM {_tableName} WHERE message_id = @MessageId AND user_id = @UserId AND emoji = @Emoji";
            return await _dbConnection.QueryFirstOrDefaultAsync<MessageReaction>(sql, new { MessageId = messageId, UserId = userId, Emoji = emoji });
        }

        public async Task<IEnumerable<MessageReaction>> GetByMessageIdAsync(int messageId)
        {
            var sql = $"SELECT * FROM {_tableName} WHERE message_id = @MessageId";
            return await _dbConnection.QueryAsync<MessageReaction>(sql, new { MessageId = messageId });
        }

        public async Task<MessageReaction> AddReactionAsync(int messageId, int userId, string emoji)
        {
            var reaction = new MessageReaction
            {
                message_id = messageId,
                user_id = userId,
                emoji = emoji,
                reacted_at = DateTime.UtcNow
            };

            reaction.id = await InsertAsync(reaction);
            return reaction;
        }

        public async Task<bool> RemoveReactionAsync(int messageId, int userId, string emoji)
        {
            var sql = $"DELETE FROM {_tableName} WHERE message_id = @MessageId AND user_id = @UserId AND emoji = @Emoji";
            var rowsAffected = await _dbConnection.ExecuteAsync(sql, new { MessageId = messageId, UserId = userId, Emoji = emoji });
            return rowsAffected > 0;
        }
    }
}