// Giả sử file này là Repositories/IMessagesRepo.cs
using ChatServer.Models;
using ChatServer.Repositories.Base;

namespace ChatServer.Repositories.Messenger;
// 1. Kế thừa từ IBaseRepository<Message>
//    Bây giờ IMessagesRepo sẽ tự động có các phương thức như
//    GetByIdAsync, GetAllAsync, InsertAsync, UpdateAsync, DeleteAsync.
public interface IMessageRepo : IBaseRepository<Message>
{
    // 2. Bạn chỉ cần định nghĩa thêm các phương thức ĐẶC THÙ cho Message ở đây.
    //    Ví dụ: Lấy tất cả tin nhắn trong một cuộc hội thoại.
    Task<IEnumerable<Message>> GetMessagesByConversationIdAsync(string conversationId);
    Task<Message> InsertMessageAsync(Message message);

    Task<IEnumerable<Message>> GetMessagesAfterIdAsync(int conversationId, long lastMessageId);

}