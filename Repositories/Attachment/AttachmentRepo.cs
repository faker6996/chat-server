using ChatServer.Models;
using ChatServer.Repositories.Base;
using System.Data;
using Dapper;

namespace ChatServer.Repositories.Attachment
{
    public class AttachmentRepo : BaseRepository<Models.Attachment>, IAttachmentRepo
    {
        public AttachmentRepo(IDbConnection dbConnection) : base(dbConnection)
        {
        }

        public async Task<long> CreateAsync(Models.Attachment attachment)
        {
            attachment.created_at = DateTime.UtcNow;
            return await base.InsertAsync(attachment);
        }

        public async Task<List<Models.Attachment>> GetByMessageIdAsync(long messageId)
        {
            var sql = "SELECT * FROM attachments WHERE message_id = @messageId ORDER BY created_at";
            var attachments = await _dbConnection.QueryAsync<Models.Attachment>(sql, new { messageId });
            return [.. attachments];
        }
    }
}