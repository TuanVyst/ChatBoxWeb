using BusinessObject.Entities;
using BusinessObject.Enums;

namespace Service.Interfaces
{
    public interface IChatService
    {
        /// <summary>
        /// Lưu tin nhắn mới vào DB. Kiểm tra senderId tồn tại trước khi lưu.
        /// </summary>
        /// <returns>Message đã lưu, hoặc null nếu senderId không tồn tại</returns>
        Task<Message?> SaveAndBroadcastMessageAsync(string senderId, string? content, MessageType type, string? fileUrl, string? originalFileName = null);

        /// <summary>
        /// Lấy 50 tin nhắn gần nhất
        /// </summary>
        Task<IEnumerable<Message>> GetChatHistoryAsync();

        /// <summary>
        /// Kiểm tra user có tồn tại trong hệ thống không
        /// </summary>
        Task<bool> UserExistsAsync(string userId);
    }
}
