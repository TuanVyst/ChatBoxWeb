using System.ComponentModel.DataAnnotations;

namespace BusinessObject.Dtos
{
    public class CreateUserDto
    {
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;
    }
}
