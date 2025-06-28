using Dapper;
using System.Data;
using ChatServer.Models;
using ChatServer.Repositories.Base;

namespace ChatServer.Repositories.Messenger;

public class MessageRepo : BaseRepository<Message>, IMessageRepo
{
    public MessageRepo(IDbConnection dbConnection) : base(dbConnection)
    {

    }

    public async Task<Message> InsertMessageAsync(Message message)
    {
        long newId = await InsertAsync(message);

        Console.WriteLine($"Đã insert tin nhắn thành công với ID: {newId}");

        // Bạn có thể gán ID vừa nhận được vào lại object nếu cần
        message.id = newId;
        return message;
    }

    public async Task<IEnumerable<Message>> GetMessagesByConversationIdAsync(string conversationId)
    {
        var sql = $"SELECT * FROM {_tableName} WHERE conversation_id = @ConversationId ORDER BY created_at ASC";

        return await _dbConnection.QueryAsync<Message>(sql, new { ConversationId = conversationId });
    }
}