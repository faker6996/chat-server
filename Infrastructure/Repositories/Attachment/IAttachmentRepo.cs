using ChatServer.Core.Models;
using ChatServer.Infrastructure.Repositories.Base;

namespace ChatServer.Infrastructure.Repositories.Attachment
{
    public interface IAttachmentRepo : IBaseRepository<Core.Models.Attachment>
    {
        Task<long> CreateAsync(Core.Models.Attachment attachment);
        Task<List<Core.Models.Attachment>> GetByMessageIdAsync(int messageId);
    }
}