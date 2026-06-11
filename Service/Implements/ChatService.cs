using BusinessObject.Entities;
using BusinessObject.Enums;
using Repository.Interfaces;
using Service.Interfaces;

namespace Service.Implements
{
    public class ChatService : IChatService
    {
        private readonly IMessageRepository _messageRepo;
        private readonly IUserRepository _userRepo;

        public ChatService(IMessageRepository messageRepo, IUserRepository userRepo)
        {
            _messageRepo = messageRepo;
            _userRepo = userRepo;
        }

        public async Task<Message?> SaveAndBroadcastMessageAsync(string senderId, string? content, MessageType type, string? fileUrl)
        {
            // Kiểm tra senderId có tồn tại trong hệ thống không
            var user = await _userRepo.GetUserByIdAsync(senderId);
            if (user == null)
                return null; // User không tồn tại

            var newMessage = new Message
            {
                SenderId = senderId,
                Content = content,
                Type = type,
                FileUrl = fileUrl,
                Timestamp = DateTime.UtcNow
            };

            return await _messageRepo.AddMessageAsync(newMessage);
        }

        public async Task<IEnumerable<Message>> GetChatHistoryAsync()
        {
            return await _messageRepo.GetMessageHistoryAsync(50);
        }

        public async Task<bool> UserExistsAsync(string userId)
        {
            var user = await _userRepo.GetUserByIdAsync(userId);
            return user != null;
        }
    }
}
