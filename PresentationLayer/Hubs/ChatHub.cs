using BusinessObject.Enums;
using Microsoft.AspNetCore.SignalR;
using Service.Interfaces;

namespace PresentationLayer.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;

        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
        }

        // Hàm này Frontend sẽ gọi để gửi tin nhắn
        public async Task SendMessage(string senderId, string content, int messageType, string fileUrl)
        {
            // Ép kiểu từ int sang Enum MessageType
            var type = (MessageType)messageType;

            // 1. Lưu tin nhắn vào Database thông qua Service
            var savedMessage = await _chatService.SaveAndBroadcastMessageAsync(senderId, content, type, fileUrl);

            // 2. Phát tin nhắn vừa lưu đến TẤT CẢ client đang kết nối
            // Frontend sẽ lắng nghe sự kiện "ReceiveMessage" này
            await Clients.All.SendAsync("ReceiveMessage", savedMessage);
        }
    }
}
