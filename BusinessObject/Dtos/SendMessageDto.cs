using BusinessObject.Enums;
using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Dtos
{
    public class SendMessageDto
    {
        /// <summary>
        /// ID người gửi (phải là ID của user đã tồn tại trong hệ thống)
        /// </summary>
        [Required]
        public string SenderId { get; set; } = string.Empty;

        /// <summary>
        /// Nội dung tin nhắn (bắt buộc với tin nhắn Text)
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// Loại tin nhắn: Text, Image, File
        /// </summary>
        public MessageType Type { get; set; } = MessageType.Text;
    }
}
