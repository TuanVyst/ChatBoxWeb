using BusinessObject.Dtos;
using BusinessObject.Enums;
using Microsoft.AspNetCore.SignalR;
using Service.Interfaces;
using System.Collections.Concurrent;

namespace PresentationLayer.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private static readonly ConcurrentDictionary<string, ConnectionEntry> _connectedUsers = new();
        private static readonly TimeSpan StaleTimeout = TimeSpan.FromSeconds(60);

        private record ConnectionEntry(string UserId, string Username, DateTime ConnectedAt);

        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
        }

        private List<(string UserId, string Username)> GetOnlineUsers()
        {
            var now = DateTime.UtcNow;

            // Xóa entries quá 60 giây (stale connections không được cleanup)
            var staleKeys = _connectedUsers
                .Where(kvp => now - kvp.Value.ConnectedAt > StaleTimeout)
                .Select(kvp => kvp.Key)
                .ToList();
            foreach (var key in staleKeys)
                _connectedUsers.TryRemove(key, out _);

            return _connectedUsers.Values
                .GroupBy(e => e.UserId)
                .Select(g => g.First())
                .Select(e => (e.UserId, e.Username))
                .ToList();
        }

        public async Task JoinChat(string userId, string username)
        {
            // Xóa entries cũ cùng userId (tránh duplicate khi reconnect)
            var staleKeys = _connectedUsers
                .Where(kvp => kvp.Value.UserId == userId)
                .Select(kvp => kvp.Key)
                .ToList();
            foreach (var key in staleKeys)
                _connectedUsers.TryRemove(key, out _);

            _connectedUsers[Context.ConnectionId] = new ConnectionEntry(userId, username, DateTime.UtcNow);

            var onlineUsers = GetOnlineUsers().Select(u => new { u.UserId, u.Username }).ToList();
            await Clients.All.SendAsync("OnlineUsers", onlineUsers);
            await Clients.Others.SendAsync("UserOnline", userId, username);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_connectedUsers.TryRemove(Context.ConnectionId, out var entry))
            {
                var onlineUsers = GetOnlineUsers().Select(u => new { u.UserId, u.Username }).ToList();
                await Clients.All.SendAsync("OnlineUsers", onlineUsers);
                await Clients.All.SendAsync("UserOffline", entry.UserId, entry.Username);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string senderId, string content, int messageType, string? fileUrl)
        {
            var type = (MessageType)messageType;

            var savedMessage = await _chatService.SaveAndBroadcastMessageAsync(senderId, content, type, fileUrl);

            if (savedMessage == null)
            {
                await Clients.Caller.SendAsync("Error", $"Không tìm thấy user với SenderId: {senderId}");
                return;
            }

            var response = new MessageResponseDto
            {
                Id = savedMessage.Id,
                SenderId = savedMessage.SenderId,
                SenderUsername = savedMessage.Sender?.Username ?? "Unknown",
                Content = savedMessage.Content,
                Type = savedMessage.Type,
                FileUrl = savedMessage.FileUrl,
                OriginalFileName = savedMessage.OriginalFileName,
                Timestamp = savedMessage.Timestamp
            };

            await Clients.All.SendAsync("ReceiveMessage", response);
        }
    }
}
