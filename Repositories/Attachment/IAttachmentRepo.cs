using ChatServer.Models;

namespace ChatServer.Repositories.Attachment
{
    public interface IAttachmentRepo
    {
        Task<long> CreateAsync(Models.Attachment attachment);
        Task<List<Models.Attachment>> GetByMessageIdAsync(long messageId);
    }
}