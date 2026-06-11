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

        // Frontend gọi hàm này để gửi tin nhắn qua SignalR
        public async Task SendMessage(string senderId, string content, int messageType, string? fileUrl)
        {
            var type = (MessageType)messageType;

            var savedMessage = await _chatService.SaveAndBroadcastMessageAsync(senderId, content, type, fileUrl);

            if (savedMessage == null)
            {
                // Gửi lỗi về cho client nếu senderId không tồn tại
                await Clients.Caller.SendAsync("Error", $"Không tìm thấy user với SenderId: {senderId}");
                return;
            }

            await Clients.All.SendAsync("ReceiveMessage", savedMessage);
        }
    }
}
