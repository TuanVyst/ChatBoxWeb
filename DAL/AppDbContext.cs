using BusinessObject.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Định nghĩa các bảng trong Database
        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình mối quan hệ 1 - Nhiều (One-to-Many) giữa User và Message
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.Messages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Cascade); // Nếu xóa User, tự động xóa tất cả Message của User đó

            // Lưu ý với SQLite: Enum sẽ tự động được lưu dưới dạng số nguyên (0, 1, 2) trong DB để tối ưu dung lượng
        }
    }
}
