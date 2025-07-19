using ChatServer.Core.Models;
using ChatServer.Infrastructure.Repositories.Attachment;
using ChatServer.Infrastructure.Repositories.Base;
using System.Data;
using Dapper;

namespace ChatServer.Infrastructure.Repositories.Attachment
{
    public class AttachmentRepo : BaseRepository<Attachment>, IAttachmentRepo
    {
        public AttachmentRepo(IDbConnection dbConnection) : base(dbConnection)
        {
        }

        public async Task<long> CreateAsync(Attachment attachment)
        {
            attachment.created_at = DateTime.UtcNow;
            return await base.InsertWithLongIdAsync(attachment);
        }

        public async Task<List<Models.Attachment>> GetByMessageIdAsync(int messageId)
        {
            var sql = "SELECT * FROM attachments WHERE message_id = @messageId ORDER BY created_at";
            var attachments = await _dbConnection.QueryAsync<Models.Attachment>(sql, new { messageId });
            return [.. attachments];
        }
    }
}