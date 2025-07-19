using Dapper;
using System.Data;
using ChatServer.Core.Models;
using ChatServer.Infrastructure.Repositories.Base;

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

    public async Task<Conversation?> GetConversationByIdAsync(int conversationId)
    {
        var sql = "SELECT * FROM conversations WHERE id = @ConversationId";
        return await _dbConnection.QueryFirstOrDefaultAsync<Conversation>(sql, new { ConversationId = conversationId });
    }

    public async Task<Conversation?> GetPrivateConversationAsync(int userId1, int userId2)
    {
        var sql = @"
            SELECT c.* FROM conversations c
            INNER JOIN conversation_participants cp1 ON c.id = cp1.conversation_id AND cp1.user_id = @UserId1
            INNER JOIN conversation_participants cp2 ON c.id = cp2.conversation_id AND cp2.user_id = @UserId2
            WHERE c.is_group = false";
        
        return await _dbConnection.QueryFirstOrDefaultAsync<Conversation>(sql, new { UserId1 = userId1, UserId2 = userId2 });
    }

    public async Task<Conversation> CreateConversationAsync(Conversation conversation)
    {
        var sql = @"
            INSERT INTO conversations (is_group, name, description, avatar_url, max_members, is_public, invite_link, require_approval, created_by, created_at)
            VALUES (@is_group, @name, @description, @avatar_url, @max_members, @is_public, @invite_link, @require_approval, @created_by, @created_at);
            SELECT LAST_INSERT_ID();";
        
        int newId = await _dbConnection.QueryFirstAsync<int>(sql, conversation);
        conversation.id = newId;
        return conversation;
    }

    public async Task AddConversationParticipantAsync(int conversationId, int userId)
    {
        var sql = @"
            INSERT INTO conversation_participants (conversation_id, user_id, joined_at, role)
            VALUES (@ConversationId, @UserId, @JoinedAt, @Role)";
        
        await _dbConnection.ExecuteAsync(sql, new {
            ConversationId = conversationId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow,
            Role = "member"
        });
    }
}