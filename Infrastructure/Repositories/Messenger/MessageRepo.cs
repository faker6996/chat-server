using Dapper;
using System.Data;
using ChatServer.Models;
using ChatServer.Repositories.Base;

namespace ChatServer.Infrastructure.Repositories.Messenger;

public class MessageRepo : BaseRepository<Message>, IMessageRepo
{
    public MessageRepo(IDbConnection dbConnection) : base(dbConnection)
    {

    }

    public async Task<Message> InsertMessageAsync(Message message)
    {
        int newId = await InsertAsync(message);

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

    public async Task<IEnumerable<Message>> GetMessagesAfterIdAsync(int conversationId, int lastMessageId)
    {
        // _tableName và _dbConnection được kế thừa từ BaseRepository
        var sql = $"SELECT * FROM {_tableName} " +
                  "WHERE conversation_id = @ConversationId AND id > @LastMessageId " +
                  "ORDER BY created_at ASC";

        return await _dbConnection.QueryAsync<Message>(sql, new { ConversationId = conversationId, LastMessageId = lastMessageId });
    }

    public async Task<Message?> GetMessageWithDetailsAsync(int messageId)
    {
        var sql = $@"
            SELECT 
                m.*,
                rm.id as replied_id,
                rm.content as replied_content,
                rm.sender_id as replied_sender_id,
                rm.content_type as replied_content_type,
                rm.created_at as replied_created_at
            FROM {_tableName} m
            LEFT JOIN {_tableName} rm ON m.reply_to_message_id = rm.id
            WHERE m.id = @MessageId";

        var messageDictionary = new Dictionary<int, Message>();
        
        var result = await _dbConnection.QueryAsync<Message, dynamic, Message>(
            sql,
            (message, repliedMessage) =>
            {
                if (!messageDictionary.TryGetValue(message.id, out var messageEntry))
                {
                    messageEntry = message;
                    messageDictionary.Add(message.id, messageEntry);
                }
                
                return messageEntry;
            },
            new { MessageId = messageId },
            splitOn: "replied_id"
        );

        return result.FirstOrDefault();
    }
}