using BusinessObject.Enums;

namespace BusinessObject.Dtos
{
    public class MessageResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string SenderUsername { get; set; } = string.Empty;
        public string? Content { get; set; }
        public MessageType Type { get; set; }
        public string? FileUrl { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
