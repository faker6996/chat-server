using ChatServer.Models;
using ChatServer.Repositories.Base;

namespace ChatServer.Infrastructure.Repositories.Attachment
{
    public interface IAttachmentRepo : IBaseRepository<Models.Attachment>
    {
        Task<long> CreateAsync(Models.Attachment attachment);
        Task<List<Models.Attachment>> GetByMessageIdAsync(int messageId);
    }
}