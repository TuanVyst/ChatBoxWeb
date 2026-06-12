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
        // Smileys & Emotion
        new() { Code = "😀", Name = "Grinning Face", Category = "Smileys" },
        new() { Code = "😃", Name = "Grinning Face with Big Eyes", Category = "Smileys" },
        new() { Code = "😄", Name = "Grinning Face with Smiling Eyes", Category = "Smileys" },
        new() { Code = "😁", Name = "Beaming Face with Smiling Eyes", Category = "Smileys" },
        new() { Code = "😆", Name = "Grinning Squinting Face", Category = "Smileys" },
        new() { Code = "😅", Name = "Grinning Face with Sweat", Category = "Smileys" },
        new() { Code = "🤣", Name = "Rolling on the Floor Laughing", Category = "Smileys" },
        new() { Code = "😂", Name = "Face with Tears of Joy", Category = "Smileys" },
        new() { Code = "🙂", Name = "Slightly Smiling Face", Category = "Smileys" },
        new() { Code = "😊", Name = "Smiling Face with Smiling Eyes", Category = "Smileys" },
        new() { Code = "😇", Name = "Smiling Face with Halo", Category = "Smileys" },
        new() { Code = "🥰", Name = "Smiling Face with Hearts", Category = "Smileys" },
        new() { Code = "😍", Name = "Heart Eyes", Category = "Smileys" },
        new() { Code = "🤩", Name = "Star-Struck", Category = "Smileys" },
        new() { Code = "😘", Name = "Face Blowing a Kiss", Category = "Smileys" },
        new() { Code = "😗", Name = "Kissing Face", Category = "Smileys" },
        new() { Code = "😚", Name = "Kissing Face with Closed Eyes", Category = "Smileys" },
        new() { Code = "😙", Name = "Kissing Face with Smiling Eyes", Category = "Smileys" },
        new() { Code = "🥲", Name = "Smiling Face with Tear", Category = "Smileys" },
        new() { Code = "😋", Name = "Face Savoring Food", Category = "Smileys" },
        new() { Code = "😛", Name = "Face with Tongue", Category = "Smileys" },
        new() { Code = "😜", Name = "Winking Face with Tongue", Category = "Smileys" },
        new() { Code = "🤪", Name = "Zany Face", Category = "Smileys" },
        new() { Code = "😝", Name = "Squinting Face with Tongue", Category = "Smileys" },
        new() { Code = "🤑", Name = "Money-Mouth Face", Category = "Smileys" },
        new() { Code = "🤗", Name = "Hugging Face", Category = "Smileys" },
        new() { Code = "🤭", Name = "Face with Hand Over Mouth", Category = "Smileys" },
        new() { Code = "🫢", Name = "Face with Open Eyes and Hand Over Mouth", Category = "Smileys" },
        new() { Code = "🫣", Name = "Face with Peeking Eye", Category = "Smileys" },
        new() { Code = "🤫", Name = "Shushing Face", Category = "Smileys" },
        new() { Code = "🤔", Name = "Thinking Face", Category = "Smileys" },
        new() { Code = "🫡", Name = "Saluting Face", Category = "Smileys" },
        new() { Code = "🤐", Name = "Zipper-Mouth Face", Category = "Smileys" },
        new() { Code = "🤨", Name = "Face with Raised Eyebrow", Category = "Smileys" },
        new() { Code = "😐", Name = "Neutral Face", Category = "Smileys" },
        new() { Code = "😑", Name = "Expressionless Face", Category = "Smileys" },
        new() { Code = "😶", Name = "Face Without Mouth", Category = "Smileys" },
        new() { Code = "😏", Name = "Smirking Face", Category = "Smileys" },
        new() { Code = "😒", Name = "Unamused Face", Category = "Smileys" },
        new() { Code = "🙄", Name = "Face with Rolling Eyes", Category = "Smileys" },
        new() { Code = "😬", Name = "Grimacing Face", Category = "Smileys" },
        new() { Code = "😮", Name = "Face with Open Mouth", Category = "Smileys" },
        new() { Code = "😯", Name = "Hushed Face", Category = "Smileys" },
        new() { Code = "😲", Name = "Astonished Face", Category = "Smileys" },
        new() { Code = "😳", Name = "Flushed Face", Category = "Smileys" },
        new() { Code = "🥺", Name = "Pleading Face", Category = "Smileys" },
        new() { Code = "😢", Name = "Crying Face", Category = "Smileys" },
        new() { Code = "😭", Name = "Loudly Crying Face", Category = "Smileys" },
        new() { Code = "😤", Name = "Face with Steam From Nose", Category = "Smileys" },
        new() { Code = "😠", Name = "Angry Face", Category = "Smileys" },
        new() { Code = "😡", Name = "Pouting Face", Category = "Smileys" },
        new() { Code = "🤬", Name = "Face with Symbols on Mouth", Category = "Smileys" },
        new() { Code = "😈", Name = "Smiling Face with Horns", Category = "Smileys" },
        new() { Code = "👿", Name = "Angry Face with Horns", Category = "Smileys" },
        new() { Code = "💀", Name = "Skull", Category = "Smileys" },
        new() { Code = "☠️", Name = "Skull and Crossbones", Category = "Smileys" },
        new() { Code = "💩", Name = "Pile of Poo", Category = "Smileys" },
        new() { Code = "🤡", Name = "Clown Face", Category = "Smileys" },
        new() { Code = "👹", Name = "Ogre", Category = "Smileys" },
        new() { Code = "👺", Name = "Goblin", Category = "Smileys" },
        new() { Code = "👻", Name = "Ghost", Category = "Smileys" },
        new() { Code = "👽", Name = "Alien", Category = "Smileys" },
        new() { Code = "👾", Name = "Alien Monster", Category = "Smileys" },
        new() { Code = "🤖", Name = "Robot", Category = "Smileys" },
        new() { Code = "😺", Name = "Grinning Cat", Category = "Smileys" },
        new() { Code = "😸", Name = "Grinning Cat with Smiling Eyes", Category = "Smileys" },
        new() { Code = "😹", Name = "Cat with Tears of Joy", Category = "Smileys" },
        new() { Code = "😻", Name = "Smiling Cat with Heart Eyes", Category = "Smileys" },
        new() { Code = "😼", Name = "Cat with Wry Smile", Category = "Smileys" },
        new() { Code = "😽", Name = "Kissing Cat", Category = "Smileys" },
        new() { Code = "🙀", Name = "Weary Cat", Category = "Smileys" },
        new() { Code = "😿", Name = "Crying Cat", Category = "Smileys" },
        new() { Code = "😾", Name = "Pouting Cat", Category = "Smileys" },

        // Gestures & Body
        new() { Code = "👋", Name = "Waving Hand", Category = "Gestures" },
        new() { Code = "🤚", Name = "Raised Back of Hand", Category = "Gestures" },
        new() { Code = "🖐️", Name = "Hand with Fingers Splayed", Category = "Gestures" },
        new() { Code = "✋", Name = "Raised Hand", Category = "Gestures" },
        new() { Code = "🖖", Name = "Vulcan Salute", Category = "Gestures" },
        new() { Code = "🫱", Name = "Rightwards Hand", Category = "Gestures" },
        new() { Code = "🫲", Name = "Leftwards Hand", Category = "Gestures" },
        new() { Code = "🫳", Name = "Palm Down Hand", Category = "Gestures" },
        new() { Code = "🫴", Name = "Palm Up Hand", Category = "Gestures" },
        new() { Code = "👌", Name = "OK Hand", Category = "Gestures" },
        new() { Code = "🤌", Name = "Pinched Fingers", Category = "Gestures" },
        new() { Code = "🤏", Name = "Pinching Hand", Category = "Gestures" },
        new() { Code = "✌️", Name = "Victory Hand", Category = "Gestures" },
        new() { Code = "🤞", Name = "Crossed Fingers", Category = "Gestures" },
        new() { Code = "🫰", Name = "Hand with Index Finger and Thumb Crossed", Category = "Gestures" },
        new() { Code = "🤟", Name = "Love-You Gesture", Category = "Gestures" },
        new() { Code = "🤘", Name = "Sign of the Horns", Category = "Gestures" },
        new() { Code = "🤙", Name = "Call Me Hand", Category = "Gestures" },
        new() { Code = "👈", Name = "Backhand Index Pointing Left", Category = "Gestures" },
        new() { Code = "👉", Name = "Backhand Index Pointing Right", Category = "Gestures" },
        new() { Code = "👆", Name = "Backhand Index Pointing Up", Category = "Gestures" },
        new() { Code = "🖕", Name = "Middle Finger", Category = "Gestures" },
        new() { Code = "👇", Name = "Backhand Index Pointing Down", Category = "Gestures" },
        new() { Code = "☝️", Name = "Index Pointing Up", Category = "Gestures" },
        new() { Code = "🫵", Name = "Index Pointing at the Viewer", Category = "Gestures" },
        new() { Code = "👍", Name = "Thumbs Up", Category = "Gestures" },
        new() { Code = "👎", Name = "Thumbs Down", Category = "Gestures" },
        new() { Code = "✊", Name = "Raised Fist", Category = "Gestures" },
        new() { Code = "👊", Name = "Oncoming Fist", Category = "Gestures" },
        new() { Code = "🤛", Name = "Left-Facing Fist", Category = "Gestures" },
        new() { Code = "🤜", Name = "Right-Facing Fist", Category = "Gestures" },
        new() { Code = "👏", Name = "Clapping Hands", Category = "Gestures" },
        new() { Code = "🙌", Name = "Raising Hands", Category = "Gestures" },
        new() { Code = "🫶", Name = "Heart Hands", Category = "Gestures" },
        new() { Code = "👐", Name = "Open Hands", Category = "Gestures" },
        new() { Code = "🤲", Name = "Palms Up Together", Category = "Gestures" },
        new() { Code = "🤝", Name = "Handshake", Category = "Gestures" },
        new() { Code = "🙏", Name = "Folded Hands", Category = "Gestures" },
        new() { Code = "✍️", Name = "Writing Hand", Category = "Gestures" },
        new() { Code = "💅", Name = "Nail Polish", Category = "Gestures" },
        new() { Code = "🤳", Name = "Selfie", Category = "Gestures" },
        new() { Code = "💪", Name = "Flexed Biceps", Category = "Gestures" },
        new() { Code = "🦵", Name = "Leg", Category = "Gestures" },
        new() { Code = "🦶", Name = "Foot", Category = "Gestures" },
        new() { Code = "👂", Name = "Ear", Category = "Gestures" },
        new() { Code = "👃", Name = "Nose", Category = "Gestures" },
        new() { Code = "🧠", Name = "Brain", Category = "Gestures" },
        new() { Code = "🫀", Name = "Anatomical Heart", Category = "Gestures" },
        new() { Code = "🫁", Name = "Lungs", Category = "Gestures" },
        new() { Code = "👀", Name = "Eyes", Category = "Gestures" },
        new() { Code = "👁️", Name = "Eye", Category = "Gestures" },
        new() { Code = "👅", Name = "Tongue", Category = "Gestures" },
        new() { Code = "👄", Name = "Mouth", Category = "Gestures" },

        // Love & Romance
        new() { Code = "❤️", Name = "Red Heart", Category = "Love" },
        new() { Code = "🩷", Name = "Pink Heart", Category = "Love" },
        new() { Code = "🧡", Name = "Orange Heart", Category = "Love" },
        new() { Code = "💛", Name = "Yellow Heart", Category = "Love" },
        new() { Code = "💚", Name = "Green Heart", Category = "Love" },
        new() { Code = "💙", Name = "Blue Heart", Category = "Love" },
        new() { Code = "🩵", Name = "Light Blue Heart", Category = "Love" },
        new() { Code = "💜", Name = "Purple Heart", Category = "Love" },
        new() { Code = "🤎", Name = "Brown Heart", Category = "Love" },
        new() { Code = "🖤", Name = "Black Heart", Category = "Love" },
        new() { Code = "🩶", Name = "Grey Heart", Category = "Love" },
        new() { Code = "🤍", Name = "White Heart", Category = "Love" },
        new() { Code = "💔", Name = "Broken Heart", Category = "Love" },
        new() { Code = "❤️‍🔥", Name = "Heart on Fire", Category = "Love" },
        new() { Code = "❤️‍🩹", Name = "Mending Heart", Category = "Love" },
        new() { Code = "💕", Name = "Two Hearts", Category = "Love" },
        new() { Code = "💞", Name = "Revolving Hearts", Category = "Love" },
        new() { Code = "💓", Name = "Beating Heart", Category = "Love" },
        new() { Code = "💗", Name = "Growing Heart", Category = "Love" },
        new() { Code = "💖", Name = "Sparkling Heart", Category = "Love" },
        new() { Code = "💘", Name = "Heart with Arrow", Category = "Love" },
        new() { Code = "💝", Name = "Heart with Ribbon", Category = "Love" },
        new() { Code = "💟", Name = "Heart Decoration", Category = "Love" },
        new() { Code = "♥️", Name = "Heart Suit", Category = "Love" },
        new() { Code = "💌", Name = "Love Letter", Category = "Love" },
        new() { Code = "💏", Name = "Kiss", Category = "Love" },
        new() { Code = "👩‍❤️‍💋‍👨", Name = "Kiss: Woman, Man", Category = "Love" },
        new() { Code = "💑", Name = "Couple with Heart", Category = "Love" },
        new() { Code = "👩‍❤️‍👨", Name = "Couple with Heart: Woman, Man", Category = "Love" },
        new() { Code = "💒", Name = "Wedding", Category = "Love" },

        // Animals & Nature
        new() { Code = "🐶", Name = "Dog Face", Category = "Animals" },
        new() { Code = "🐱", Name = "Cat Face", Category = "Animals" },
        new() { Code = "🐭", Name = "Mouse Face", Category = "Animals" },
        new() { Code = "🐹", Name = "Hamster", Category = "Animals" },
        new() { Code = "🐰", Name = "Rabbit Face", Category = "Animals" },
        new() { Code = "🦊", Name = "Fox", Category = "Animals" },
        new() { Code = "🐻", Name = "Bear", Category = "Animals" },
        new() { Code = "🐼", Name = "Panda", Category = "Animals" },
        new() { Code = "🐨", Name = "Koala", Category = "Animals" },
        new() { Code = "🐯", Name = "Tiger Face", Category = "Animals" },
        new() { Code = "🦁", Name = "Lion", Category = "Animals" },
        new() { Code = "🐮", Name = "Cow Face", Category = "Animals" },
        new() { Code = "🐷", Name = "Pig Face", Category = "Animals" },
        new() { Code = "🐸", Name = "Frog", Category = "Animals" },
        new() { Code = "🐵", Name = "Monkey Face", Category = "Animals" },
        new() { Code = "🐒", Name = "Monkey", Category = "Animals" },
        new() { Code = "🐔", Name = "Chicken", Category = "Animals" },
        new() { Code = "🐧", Name = "Penguin", Category = "Animals" },
        new() { Code = "🐦", Name = "Bird", Category = "Animals" },
        new() { Code = "🐤", Name = "Baby Chick", Category = "Animals" },
        new() { Code = "🦆", Name = "Duck", Category = "Animals" },
        new() { Code = "🦅", Name = "Eagle", Category = "Animals" },
        new() { Code = "🦉", Name = "Owl", Category = "Animals" },
        new() { Code = "🦇", Name = "Bat", Category = "Animals" },
        new() { Code = "🐺", Name = "Wolf", Category = "Animals" },
        new() { Code = "🐗", Name = "Boar", Category = "Animals" },
        new() { Code = "🐴", Name = "Horse Face", Category = "Animals" },
        new() { Code = "🦄", Name = "Unicorn", Category = "Animals" },
        new() { Code = "🐝", Name = "Honeybee", Category = "Animals" },
        new() { Code = "🦋", Name = "Butterfly", Category = "Animals" },
        new() { Code = "🐌", Name = "Snail", Category = "Animals" },
        new() { Code = "🐞", Name = "Lady Beetle", Category = "Animals" },
        new() { Code = "🐜", Name = "Ant", Category = "Animals" },
        new() { Code = "🦗", Name = "Cricket", Category = "Animals" },
        new() { Code = "🪲", Name = "Beetle", Category = "Animals" },
        new() { Code = "🦟", Name = "Mosquito", Category = "Animals" },
        new() { Code = "🦠", Name = "Microbe", Category = "Animals" },
        new() { Code = "💐", Name = "Bouquet", Category = "Animals" },
        new() { Code = "🌸", Name = "Cherry Blossom", Category = "Animals" },
        new() { Code = "💮", Name = "White Flower", Category = "Animals" },
        new() { Code = "🪷", Name = "Lotus", Category = "Animals" },
        new() { Code = "🌹", Name = "Rose", Category = "Animals" },
        new() { Code = "🌺", Name = "Hibiscus", Category = "Animals" },
        new() { Code = "🌻", Name = "Sunflower", Category = "Animals" },
        new() { Code = "🌷", Name = "Tulip", Category = "Animals" },
        new() { Code = "🌱", Name = "Seedling", Category = "Animals" },
        new() { Code = "🌲", Name = "Evergreen Tree", Category = "Animals" },
        new() { Code = "🌳", Name = "Deciduous Tree", Category = "Animals" },
        new() { Code = "🌿", Name = "Herb", Category = "Animals" },
        new() { Code = "🍀", Name = "Four Leaf Clover", Category = "Animals" },
        new() { Code = "🍁", Name = "Maple Leaf", Category = "Animals" },
        new() { Code = "🍂", Name = "Fallen Leaf", Category = "Animals" },
        new() { Code = "🍃", Name = "Leaf Fluttering in Wind", Category = "Animals" },

        // Food & Drink
        new() { Code = "🍎", Name = "Red Apple", Category = "Food" },
        new() { Code = "🍐", Name = "Pear", Category = "Food" },
        new() { Code = "🍊", Name = "Tangerine", Category = "Food" },
        new() { Code = "🍋", Name = "Lemon", Category = "Food" },
        new() { Code = "🍌", Name = "Banana", Category = "Food" },
        new() { Code = "🍉", Name = "Watermelon", Category = "Food" },
        new() { Code = "🍇", Name = "Grapes", Category = "Food" },
        new() { Code = "🍓", Name = "Strawberry", Category = "Food" },
        new() { Code = "🫐", Name = "Blueberries", Category = "Food" },
        new() { Code = "🍈", Name = "Melon", Category = "Food" },
        new() { Code = "🍒", Name = "Cherries", Category = "Food" },
        new() { Code = "🍑", Name = "Peach", Category = "Food" },
        new() { Code = "🥭", Name = "Mango", Category = "Food" },
        new() { Code = "🍍", Name = "Pineapple", Category = "Food" },
        new() { Code = "🥝", Name = "Kiwi", Category = "Food" },
        new() { Code = "🥑", Name = "Avocado", Category = "Food" },
        new() { Code = "🍆", Name = "Eggplant", Category = "Food" },
        new() { Code = "🥕", Name = "Carrot", Category = "Food" },
        new() { Code = "🌽", Name = "Corn", Category = "Food" },
        new() { Code = "🥦", Name = "Broccoli", Category = "Food" },
        new() { Code = "🧄", Name = "Garlic", Category = "Food" },
        new() { Code = "🧅", Name = "Onion", Category = "Food" },
        new() { Code = "🍄", Name = "Mushroom", Category = "Food" },
        new() { Code = "🥩", Name = "Cut of Meat", Category = "Food" },
        new() { Code = "🍔", Name = "Hamburger", Category = "Food" },
        new() { Code = "🍟", Name = "French Fries", Category = "Food" },
        new() { Code = "🍕", Name = "Pizza", Category = "Food" },
        new() { Code = "🌭", Name = "Hot Dog", Category = "Food" },
        new() { Code = "🥪", Name = "Sandwich", Category = "Food" },
        new() { Code = "🌮", Name = "Taco", Category = "Food" },
        new() { Code = "🌯", Name = "Burrito", Category = "Food" },
        new() { Code = "🥗", Name = "Green Salad", Category = "Food" },
        new() { Code = "🍜", Name = "Steaming Bowl", Category = "Food" },
        new() { Code = "🍝", Name = "Spaghetti", Category = "Food" },
        new() { Code = "🍣", Name = "Sushi", Category = "Food" },
        new() { Code = "🍤", Name = "Fried Shrimp", Category = "Food" },
        new() { Code = "🍚", Name = "Cooked Rice", Category = "Food" },
        new() { Code = "🍛", Name = "Curry Rice", Category = "Food" },
        new() { Code = "🍜", Name = "Steaming Bowl", Category = "Food" },
        new() { Code = "🍰", Name = "Shortcake", Category = "Food" },
        new() { Code = "🧁", Name = "Cupcake", Category = "Food" },
        new() { Code = "🍦", Name = "Soft Ice Cream", Category = "Food" },
        new() { Code = "🍨", Name = "Ice Cream", Category = "Food" },
        new() { Code = "🍩", Name = "Doughnut", Category = "Food" },
        new() { Code = "🍪", Name = "Cookie", Category = "Food" },
        new() { Code = "🎂", Name = "Birthday Cake", Category = "Food" },
        new() { Code = "🍫", Name = "Chocolate", Category = "Food" },
        new() { Code = "🍬", Name = "Candy", Category = "Food" },
        new() { Code = "🍭", Name = "Lollipop", Category = "Food" },
        new() { Code = "☕", Name = "Hot Beverage", Category = "Food" },
        new() { Code = "🍵", Name = "Teacup Without Handle", Category = "Food" },
        new() { Code = "🧃", Name = "Beverage Box", Category = "Food" },
        new() { Code = "🥛", Name = "Glass of Milk", Category = "Food" },
        new() { Code = "🍺", Name = "Beer Mug", Category = "Food" },
        new() { Code = "🍻", Name = "Clinking Beer Mugs", Category = "Food" },
        new() { Code = "🥂", Name = "Clinking Glasses", Category = "Food" },
        new() { Code = "🍷", Name = "Wine Glass", Category = "Food" },
        new() { Code = "🍸", Name = "Cocktail Glass", Category = "Food" },
        new() { Code = "🍹", Name = "Tropical Drink", Category = "Food" },

        // Travel & Places
        new() { Code = "🌍", Name = "Globe Showing Europe-Africa", Category = "Travel" },
        new() { Code = "🌎", Name = "Globe Showing Americas", Category = "Travel" },
        new() { Code = "🌏", Name = "Globe Showing Asia-Australia", Category = "Travel" },
        new() { Code = "🗺️", Name = "World Map", Category = "Travel" },
        new() { Code = "🏔️", Name = "Snow-Capped Mountain", Category = "Travel" },
        new() { Code = "🏖️", Name = "Beach with Umbrella", Category = "Travel" },
        new() { Code = "🏝️", Name = "Desert Island", Category = "Travel" },
        new() { Code = "🌋", Name = "Volcano", Category = "Travel" },
        new() { Code = "🏯", Name = "Japanese Castle", Category = "Travel" },
        new() { Code = "🏰", Name = "Castle", Category = "Travel" },
        new() { Code = "🗼", Name = "Tokyo Tower", Category = "Travel" },
        new() { Code = "🗽", Name = "Statue of Liberty", Category = "Travel" },
        new() { Code = "🏠", Name = "House", Category = "Travel" },
        new() { Code = "🏡", Name = "House with Garden", Category = "Travel" },
        new() { Code = "🏢", Name = "Office Building", Category = "Travel" },
        new() { Code = "🏣", Name = "Japanese Post Office", Category = "Travel" },
        new() { Code = "🏤", Name = "Post Office", Category = "Travel" },
        new() { Code = "🏥", Name = "Hospital", Category = "Travel" },
        new() { Code = "🏦", Name = "Bank", Category = "Travel" },
        new() { Code = "🏨", Name = "Hotel", Category = "Travel" },
        new() { Code = "🏩", Name = "Love Hotel", Category = "Travel" },
        new() { Code = "🏪", Name = "Convenience Store", Category = "Travel" },
        new() { Code = "🏫", Name = "School", Category = "Travel" },
        new() { Code = "🏬", Name = "Department Store", Category = "Travel" },
        new() { Code = "🏭", Name = "Factory", Category = "Travel" },
        new() { Code = "🎠", Name = "Carousel Horse", Category = "Travel" },
        new() { Code = "🎡", Name = "Ferris Wheel", Category = "Travel" },
        new() { Code = "🎢", Name = "Roller Coaster", Category = "Travel" },
        new() { Code = "🚃", Name = "Railway Car", Category = "Travel" },
        new() { Code = "🚄", Name = "High-Speed Train", Category = "Travel" },
        new() { Code = "🚅", Name = "Bullet Train", Category = "Travel" },
        new() { Code = "🚗", Name = "Automobile", Category = "Travel" },
        new() { Code = "🚕", Name = "Taxi", Category = "Travel" },
        new() { Code = "🚙", Name = "SUV", Category = "Travel" },
        new() { Code = "🚌", Name = "Bus", Category = "Travel" },
        new() { Code = "🚎", Name = "Trolleybus", Category = "Travel" },
        new() { Code = "🏎️", Name = "Racing Car", Category = "Travel" },
        new() { Code = "🚓", Name = "Police Car", Category = "Travel" },
        new() { Code = "🚑", Name = "Ambulance", Category = "Travel" },
        new() { Code = "🚒", Name = "Fire Engine", Category = "Travel" },
        new() { Code = "🚐", Name = "Minibus", Category = "Travel" },
        new() { Code = "🚚", Name = "Delivery Truck", Category = "Travel" },
        new() { Code = "🚛", Name = "Articulated Lorry", Category = "Travel" },
        new() { Code = "🚜", Name = "Tractor", Category = "Travel" },
        new() { Code = "🛵", Name = "Motor Scooter", Category = "Travel" },
        new() { Code = "🚲", Name = "Bicycle", Category = "Travel" },
        new() { Code = "🛴", Name = "Kick Scooter", Category = "Travel" },
        new() { Code = "🚝", Name = "Monorail", Category = "Travel" },
        new() { Code = "🚟", Name = "Suspension Railway", Category = "Travel" },
        new() { Code = "🚠", Name = "Mountain Cableway", Category = "Travel" },
        new() { Code = "🚡", Name = "Aerial Tramway", Category = "Travel" },
        new() { Code = "🚀", Name = "Rocket", Category = "Travel" },
        new() { Code = "🛸", Name = "Flying Saucer", Category = "Travel" },
        new() { Code = "✈️", Name = "Airplane", Category = "Travel" },
        new() { Code = "🛩️", Name = "Small Airplane", Category = "Travel" },
        new() { Code = "🛫", Name = "Airplane Departure", Category = "Travel" },
        new() { Code = "🛬", Name = "Airplane Arrival", Category = "Travel" },
        new() { Code = "🚁", Name = "Helicopter", Category = "Travel" },
        new() { Code = "⛵", Name = "Sailboat", Category = "Travel" },
        new() { Code = "🚤", Name = "Speedboat", Category = "Travel" },
        new() { Code = "🛳️", Name = "Passenger Ship", Category = "Travel" },
        new() { Code = "⛴️", Name = "Ferry", Category = "Travel" },
        new() { Code = "⚓", Name = "Anchor", Category = "Travel" },
        new() { Code = "🪂", Name = "Parachute", Category = "Travel" },
        new() { Code = "🌄", Name = "Sunrise Over Mountains", Category = "Travel" },
        new() { Code = "🌅", Name = "Sunrise", Category = "Travel" },
        new() { Code = "🌆", Name = "Cityscape at Dusk", Category = "Travel" },
        new() { Code = "🌇", Name = "Sunset", Category = "Travel" },
        new() { Code = "🌉", Name = "Bridge at Night", Category = "Travel" },
        new() { Code = "🌌", Name = "Milky Way", Category = "Travel" },
        new() { Code = "🎆", Name = "Fireworks", Category = "Travel" },
        new() { Code = "🎇", Name = "Sparkler", Category = "Travel" },

        // Activities
        new() { Code = "⚽", Name = "Soccer Ball", Category = "Activities" },
        new() { Code = "🏀", Name = "Basketball", Category = "Activities" },
        new() { Code = "🏈", Name = "American Football", Category = "Activities" },
        new() { Code = "⚾", Name = "Baseball", Category = "Activities" },
        new() { Code = "🎾", Name = "Tennis", Category = "Activities" },
        new() { Code = "🏐", Name = "Volleyball", Category = "Activities" },
        new() { Code = "🏓", Name = "Ping Pong", Category = "Activities" },
        new() { Code = "🥊", Name = "Boxing Glove", Category = "Activities" },
        new() { Code = "🥋", Name = "Martial Arts Uniform", Category = "Activities" },
        new() { Code = "🎯", Name = "Bullseye", Category = "Activities" },
        new() { Code = "⛳", Name = "Flag in Hole", Category = "Activities" },
        new() { Code = "🎣", Name = "Fishing Pole", Category = "Activities" },
        new() { Code = "🎿", Name = "Skis", Category = "Activities" },
        new() { Code = "🛷", Name = "Sled", Category = "Activities" },
        new() { Code = "🥌", Name = "Curling Stone", Category = "Activities" },
        new() { Code = "🎮", Name = "Video Game", Category = "Activities" },
        new() { Code = "🕹️", Name = "Joystick", Category = "Activities" },
        new() { Code = "🎲", Name = "Game Die", Category = "Activities" },
        new() { Code = "♟️", Name = "Chess Pawn", Category = "Activities" },
        new() { Code = "🎭", Name = "Performing Arts", Category = "Activities" },
        new() { Code = "🎨", Name = "Artist Palette", Category = "Activities" },
        new() { Code = "🎬", Name = "Clapper Board", Category = "Activities" },
        new() { Code = "🎤", Name = "Microphone", Category = "Activities" },
        new() { Code = "🎧", Name = "Headphone", Category = "Activities" },
        new() { Code = "🎵", Name = "Musical Note", Category = "Activities" },
        new() { Code = "🎶", Name = "Musical Notes", Category = "Activities" },
        new() { Code = "🎸", Name = "Guitar", Category = "Activities" },
        new() { Code = "🎹", Name = "Musical Keyboard", Category = "Activities" },
        new() { Code = "🎺", Name = "Trumpet", Category = "Activities" },
        new() { Code = "🎻", Name = "Violin", Category = "Activities" },
        new() { Code = "🥁", Name = "Drum", Category = "Activities" },
        new() { Code = "🪘", Name = "Long Drum", Category = "Activities" },
        new() { Code = "📚", Name = "Books", Category = "Activities" },
        new() { Code = "📖", Name = "Open Book", Category = "Activities" },
        new() { Code = "📕", Name = "Closed Book", Category = "Activities" },
        new() { Code = "📗", Name = "Green Book", Category = "Activities" },
        new() { Code = "📘", Name = "Blue Book", Category = "Activities" },
        new() { Code = "📙", Name = "Orange Book", Category = "Activities" },
        new() { Code = "🎒", Name = "Backpack", Category = "Activities" },
        new() { Code = "✏️", Name = "Pencil", Category = "Activities" },
        new() { Code = "🖊️", Name = "Pen", Category = "Activities" },
        new() { Code = "📝", Name = "Memo", Category = "Activities" },

        // Symbols
        new() { Code = "🔥", Name = "Fire", Category = "Symbols" },
        new() { Code = "✅", Name = "Check Mark Button", Category = "Symbols" },
        new() { Code = "🎉", Name = "Party Popper", Category = "Symbols" },
        new() { Code = "⭐", Name = "Star", Category = "Symbols" },
        new() { Code = "🌟", Name = "Glowing Star", Category = "Symbols" },
        new() { Code = "💫", Name = "Dizzy", Category = "Symbols" },
        new() { Code = "✨", Name = "Sparkles", Category = "Symbols" },
        new() { Code = "💥", Name = "Collision", Category = "Symbols" },
        new() { Code = "💢", Name = "Anger Symbol", Category = "Symbols" },
        new() { Code = "💦", Name = "Sweat Droplets", Category = "Symbols" },
        new() { Code = "💨", Name = "Dashing Away", Category = "Symbols" },
        new() { Code = "🕳️", Name = "Hole", Category = "Symbols" },
        new() { Code = "💬", Name = "Speech Balloon", Category = "Symbols" },
        new() { Code = "👁️‍🗨️", Name = "Eye in Speech Bubble", Category = "Symbols" },
        new() { Code = "🗨️", Name = "Left Speech Bubble", Category = "Symbols" },
        new() { Code = "🗯️", Name = "Right Anger Bubble", Category = "Symbols" },
        new() { Code = "💭", Name = "Thought Balloon", Category = "Symbols" },
        new() { Code = "💤", Name = "Zzz", Category = "Symbols" },
        new() { Code = "💯", Name = "Hundred Points", Category = "Symbols" },
        new() { Code = "♻️", Name = "Recycling Symbol", Category = "Symbols" },
        new() { Code = "⚜️", Name = "Fleur-de-lis", Category = "Symbols" },
        new() { Code = "🔰", Name = "Japanese Symbol for Beginner", Category = "Symbols" },
        new() { Code = "🔱", Name = "Trident Emblem", Category = "Symbols" },
        new() { Code = "🛡️", Name = "Shield", Category = "Symbols" },
        new() { Code = "🔮", Name = "Crystal Ball", Category = "Symbols" },
        new() { Code = "🚿", Name = "Shower", Category = "Symbols" },
        new() { Code = "🛁", Name = "Bathtub", Category = "Symbols" },
        new() { Code = "🪥", Name = "Toothbrush", Category = "Symbols" },
        new() { Code = "🧴", Name = "Lotion Bottle", Category = "Symbols" },
        new() { Code = "🚰", Name = "Potable Water", Category = "Symbols" },
        new() { Code = "🪞", Name = "Mirror", Category = "Symbols" },
        new() { Code = "🪒", Name = "Razor", Category = "Symbols" },
        new() { Code = "💈", Name = "Barber Pole", Category = "Symbols" },
        new() { Code = "🏧", Name = "ATM Sign", Category = "Symbols" },
        new() { Code = "🚮", Name = "Litter in Bin Sign", Category = "Symbols" },
        new() { Code = "🚹", Name = "Men's Room", Category = "Symbols" },
        new() { Code = "🚺", Name = "Women's Room", Category = "Symbols" },
        new() { Code = "♿", Name = "Wheelchair Symbol", Category = "Symbols" },
        new() { Code = "🚭", Name = "No Smoking", Category = "Symbols" },
        new() { Code = "📵", Name = "No Mobile Phones", Category = "Symbols" },
        new() { Code = "🔞", Name = "No One Under Eighteen", Category = "Symbols" },
        new() { Code = "☢️", Name = "Radioactive", Category = "Symbols" },
        new() { Code = "☣️", Name = "Biohazard", Category = "Symbols" },
        new() { Code = "⬆️", Name = "Up Arrow", Category = "Symbols" },
        new() { Code = "⬇️", Name = "Down Arrow", Category = "Symbols" },
        new() { Code = "⬅️", Name = "Left Arrow", Category = "Symbols" },
        new() { Code = "➡️", Name = "Right Arrow", Category = "Symbols" },
        new() { Code = "🔴", Name = "Red Circle", Category = "Symbols" },
        new() { Code = "🟠", Name = "Orange Circle", Category = "Symbols" },
        new() { Code = "🟡", Name = "Yellow Circle", Category = "Symbols" },
        new() { Code = "🟢", Name = "Green Circle", Category = "Symbols" },
        new() { Code = "🔵", Name = "Blue Circle", Category = "Symbols" },
        new() { Code = "🟣", Name = "Purple Circle", Category = "Symbols" },
        new() { Code = "⚫", Name = "Black Circle", Category = "Symbols" },
        new() { Code = "⚪", Name = "White Circle", Category = "Symbols" },
        new() { Code = "🟤", Name = "Brown Circle", Category = "Symbols" },
        new() { Code = "🔶", Name = "Large Orange Diamond", Category = "Symbols" },
        new() { Code = "🔷", Name = "Large Blue Diamond", Category = "Symbols" },
        new() { Code = "🔔", Name = "Bell", Category = "Symbols" },
        new() { Code = "🔕", Name = "Bell with Slash", Category = "Symbols" },
        new() { Code = "🎵", Name = "Musical Note", Category = "Symbols" },
        new() { Code = "🎶", Name = "Multiple Musical Notes", Category = "Symbols" },
        new() { Code = "💹", Name = "Chart with Upwards Trend and Yen", Category = "Symbols" },
        new() { Code = "📛", Name = "Name Badge", Category = "Symbols" },
        new() { Code = "🔰", Name = "Japanese Symbol for Beginner", Category = "Symbols" },
        new() { Code = "💠", Name = "Diamond with a Dot", Category = "Symbols" },
        new() { Code = "🔮", Name = "Crystal Ball", Category = "Symbols" },
        new() { Code = "🛐", Name = "Place of Worship", Category = "Symbols" },
        new() { Code = "☪️", Name = "Star and Crescent", Category = "Symbols" },
        new() { Code = "☮️", Name = "Peace Symbol", Category = "Symbols" },
        new() { Code = "🕎", Name = "Menorah", Category = "Symbols" },
        new() { Code = "🔯", Name = "Dotted Six-Pointed Star", Category = "Symbols" },
        new() { Code = "♈", Name = "Aries", Category = "Symbols" },
        new() { Code = "♉", Name = "Taurus", Category = "Symbols" },
        new() { Code = "♊", Name = "Gemini", Category = "Symbols" },
        new() { Code = "♋", Name = "Cancer", Category = "Symbols" },
        new() { Code = "♌", Name = "Leo", Category = "Symbols" },
        new() { Code = "♍", Name = "Virgo", Category = "Symbols" },
        new() { Code = "♎", Name = "Libra", Category = "Symbols" },
        new() { Code = "♏", Name = "Scorpius", Category = "Symbols" },
        new() { Code = "♐", Name = "Sagittarius", Category = "Symbols" },
        new() { Code = "♑", Name = "Capricorn", Category = "Symbols" },
        new() { Code = "♒", Name = "Aquarius", Category = "Symbols" },
        new() { Code = "♓", Name = "Pisces", Category = "Symbols" },
        new() { Code = "⛎", Name = "Ophiuchus", Category = "Symbols" }
    };

            return Ok(emojis);
        }
    }
}
