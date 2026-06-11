using BusinessObject.Dtos;
using BusinessObject.Enums;
using Microsoft.AspNetCore.Mvc;
using Service.Interfaces;

namespace PresentationLayer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        /// <summary>
        /// Lấy lịch sử chat (50 tin nhắn gần nhất)
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetChatHistory()
        {
            var messages = await _chatService.GetChatHistoryAsync();

            var response = messages.Select(m => new MessageResponseDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderUsername = m.Sender?.Username ?? "Unknown",
                Content = m.Content,
                Type = m.Type,
                FileUrl = m.FileUrl,
                Timestamp = m.Timestamp
            });

            return Ok(response);
        }

        /// <summary>
        /// Gửi tin nhắn text
        /// </summary>
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.Type == MessageType.Text && string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest(new { message = "Nội dung tin nhắn không được để trống với tin nhắn Text" });

            var savedMessage = await _chatService.SaveAndBroadcastMessageAsync(
                dto.SenderId, dto.Content, dto.Type, null);

            var response = new MessageResponseDto
            {
                Id = savedMessage.Id,
                SenderId = savedMessage.SenderId,
                SenderUsername = savedMessage.Sender?.Username ?? "Unknown",
                Content = savedMessage.Content,
                Type = savedMessage.Type,
                FileUrl = savedMessage.FileUrl,
                Timestamp = savedMessage.Timestamp
            };

            return Ok(response);
        }

        /// <summary>
        /// Upload ảnh hoặc file kèm tin nhắn
        /// </summary>
        /// <param name="senderId">ID người gửi</param>
        /// <param name="content">Nội dung text kèm theo (tùy chọn)</param>
        /// <param name="type">Loại tin nhắn: 1 = Image, 2 = File</param>
        /// <param name="file">File upload</param>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(
            [FromForm] string senderId,
            [FromForm] string? content,
            [FromForm] MessageType type,
            IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Vui lòng chọn file để upload" });

            // Giới hạn 10MB
            const long maxFileSize = 10 * 1024 * 1024;
            if (file.Length > maxFileSize)
                return BadRequest(new { message = "File không được vượt quá 10MB" });

            // Kiểm tra định dạng file
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".zip", ".rar", ".txt" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
                return BadRequest(new { message = $"Định dạng file '{fileExtension}' không được hỗ trợ" });

            // Tự động xác định MessageType dựa trên extension nếu chưa chỉ định đúng
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (imageExtensions.Contains(fileExtension))
                type = MessageType.Image;
            else
                type = MessageType.File;

            // Lưu file vào wwwroot/uploads/
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Tạo tên file unique để tránh trùng
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // URL để truy cập file từ client
            var fileUrl = $"/uploads/{uniqueFileName}";

            // Lưu message vào DB
            var savedMessage = await _chatService.SaveAndBroadcastMessageAsync(
                senderId, content, type, fileUrl);

            var response = new MessageResponseDto
            {
                Id = savedMessage.Id,
                SenderId = savedMessage.SenderId,
                SenderUsername = savedMessage.Sender?.Username ?? "Unknown",
                Content = savedMessage.Content,
                Type = savedMessage.Type,
                FileUrl = savedMessage.FileUrl,
                Timestamp = savedMessage.Timestamp
            };

            return Ok(response);
        }
    }
}
