using BusinessObject.Entities;
using BusinessObject.Enums;
using Repository.Interfaces;
using Service.Interfaces;

namespace Service.Implements
{
    public class ChatService : IChatService
    {
        private readonly IMessageRepository _messageRepo;

        public ChatService(IMessageRepository messageRepo)
        {
            _messageRepo = messageRepo;
        }

        public async Task<Message> SaveAndBroadcastMessageAsync(string senderId, string? content, MessageType type, string? fileUrl)
        {
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
            return await _messageRepo.GetMessageHistoryAsync(50); // Lấy 50 tin nhắn gần nhất
        }
    }
}
