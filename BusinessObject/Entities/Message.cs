using BusinessObject.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Entities
{
    public class Message
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString(); // Dùng chuỗi GUID làm khóa chính

        [Required]
        public string SenderId { get; set; } = string.Empty; // ID người gửi

        // Khóa ngoại liên kết tới bảng User
        [ForeignKey("SenderId")]
        public User? Sender { get; set; }

        public string? Content { get; set; } // Nội dung tin nhắn chữ (hoặc text emoji)

        [Required]
        public MessageType Type { get; set; } = MessageType.Text; // Mặc định là Text

        public string? FileUrl { get; set; } // Đường dẫn lưu trên server nếu Type là Image hoặc File

        public DateTime Timestamp { get; set; } = DateTime.UtcNow; // Thời gian gửi tin (múi giờ UTC)
    }
}
