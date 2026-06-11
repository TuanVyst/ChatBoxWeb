using BusinessObject.Enums;
using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Dtos
{
    public class SendMessageDto
    {
        [Required]
        public string SenderId { get; set; } = string.Empty;

        /// <summary>
        /// Nội dung tin nhắn (bắt buộc với tin nhắn Text)
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// Loại tin nhắn: 0 = Text, 1 = Image, 2 = File
        /// </summary>
        public MessageType Type { get; set; } = MessageType.Text;
    }
}
