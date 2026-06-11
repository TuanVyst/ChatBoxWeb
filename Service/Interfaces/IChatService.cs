using BusinessObject.Entities;
using BusinessObject.Enums;

namespace Service.Interfaces
{
    public interface IChatService
    {
        /// <summary>
        /// Lưu tin nhắn mới vào DB và trả về message đã lưu (kèm thông tin Sender)
        /// </summary>
        Task<Message> SaveAndBroadcastMessageAsync(string senderId, string? content, MessageType type, string? fileUrl);

        /// <summary>
        /// Lấy 50 tin nhắn gần nhất
        /// </summary>
        Task<IEnumerable<Message>> GetChatHistoryAsync();
    }
}
