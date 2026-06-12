using BusinessObject.Dtos;
using BusinessObject.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PresentationLayer.Hubs;
using Service.Interfaces;

namespace PresentationLayer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(IChatService chatService, IHubContext<ChatHub> hubContext)
        {
            _chatService = chatService;
            _hubContext = hubContext;
        }

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
                OriginalFileName = m.OriginalFileName,
                Timestamp = m.Timestamp
            });
            return Ok(response);
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.Type == MessageType.Text && string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest(new { message = "Nội dung tin nhắn không được để trống với tin nhắn Text" });

            var savedMessage = await _chatService.SaveAndBroadcastMessageAsync(
                dto.SenderId, dto.Content, dto.Type, null);

            if (savedMessage == null)
                return NotFound(new { message = $"Không tìm thấy user với SenderId: {dto.SenderId}" });

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

            return Ok(response);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(
            [FromForm] string senderId,
            [FromForm] string? content,
            IFormFile file)
        {
            if (string.IsNullOrWhiteSpace(senderId))
                return BadRequest(new { message = "Vui lòng nhập SenderId" });

            var userExists = await _chatService.UserExistsAsync(senderId);
            if (!userExists)
                return NotFound(new { message = $"Không tìm thấy user với SenderId: {senderId}" });

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Vui lòng chọn file để upload" });

            // 500MB limit
            const long maxFileSize = 500L * 1024 * 1024;
            if (file.Length > maxFileSize)
                return BadRequest(new { message = "File không được vượt quá 500MB" });

            var allowedExtensions = new[]
            {
                ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg",
                ".mp4", ".avi", ".mov", ".mkv", ".webm",
                ".mp3", ".wav", ".ogg", ".flac", ".m4a",
                ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
                ".zip", ".rar", ".7z", ".tar", ".gz",
                ".txt", ".csv", ".json", ".xml"
            };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
                return BadRequest(new { message = $"Định dạng file '{fileExtension}' không được hỗ trợ" });

            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".svg" };
            var type = imageExtensions.Contains(fileExtension) ? MessageType.Image : MessageType.File;

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileUrl = $"/uploads/{uniqueFileName}";

            var savedMessage = await _chatService.SaveAndBroadcastMessageAsync(
                senderId, content, type, fileUrl, file.FileName);

            if (savedMessage == null)
                return NotFound(new { message = $"Không tìm thấy user với SenderId: {senderId}" });

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

            // Broadcast file message to all clients via SignalR
            await _hubContext.Clients.All.SendAsync("ReceiveMessage", response);

            return Ok(response);
        }

        /// <summary>
        /// Lấy danh sách emoji có sẵn
        /// </summary>
        [HttpGet("emojis")]
        public IActionResult GetEmojis()
        {
            var emojis = new List<EmojiDto>
    {
        new() { Code = "😀", Name = "Grinning Face", Category = "Smileys" },
        new() { Code = "😂", Name = "Face with Tears of Joy", Category = "Smileys" },
        new() { Code = "😍", Name = "Heart Eyes", Category = "Smileys" },
        new() { Code = "😭", Name = "Crying Face", Category = "Smileys" },
        new() { Code = "😡", Name = "Angry Face", Category = "Smileys" },

        new() { Code = "👍", Name = "Thumbs Up", Category = "Gestures" },
        new() { Code = "👎", Name = "Thumbs Down", Category = "Gestures" },
        new() { Code = "👏", Name = "Clapping Hands", Category = "Gestures" },
        new() { Code = "🙏", Name = "Folded Hands", Category = "Gestures" },

        new() { Code = "❤️", Name = "Red Heart", Category = "Love" },
        new() { Code = "🔥", Name = "Fire", Category = "Symbols" },
        new() { Code = "✅", Name = "Check", Category = "Symbols" },
        new() { Code = "🎉", Name = "Party Popper", Category = "Symbols" }
    };

            return Ok(emojis);
        }
    }
}
