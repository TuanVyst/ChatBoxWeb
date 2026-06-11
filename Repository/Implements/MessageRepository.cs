using BusinessObject.Entities;
using DAL;
using Microsoft.EntityFrameworkCore;
using Repository.Interfaces;


namespace Repository.Implements
{
    public class MessageRepository : IMessageRepository
    {
        private readonly AppDbContext _context;

        public MessageRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Message> AddMessageAsync(Message message)
        {
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Nạp thêm thông tin User (Avatar, Username) để trả về cho Frontend hiển thị
            await _context.Entry(message).Reference(m => m.Sender).LoadAsync();
            return message;
        }

        public async Task<IEnumerable<Message>> GetMessageHistoryAsync(int limit = 50)
        {
            return await _context.Messages
                .Include(m => m.Sender) // Lấy kèm thông tin người gửi
                .OrderByDescending(m => m.Timestamp) // Lấy tin nhắn mới nhất
                .Take(limit)
                .OrderBy(m => m.Timestamp) // Đảo ngược lại để hiển thị từ trên xuống dưới theo thời gian
                .ToListAsync();
        }
    }
}

