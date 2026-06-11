using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Entities
{
    public class User
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString(); // Dùng chuỗi GUID làm khóa chính cho linh hoạt

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

     

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

       
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
