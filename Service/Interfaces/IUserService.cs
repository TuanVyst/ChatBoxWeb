using BusinessObject.Dtos;
using BusinessObject.Entities;

namespace Service.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<UserResponseDto>> GetAllUsersAsync();
        Task<UserResponseDto?> GetUserByIdAsync(string id);
        Task<UserResponseDto> LoginAsync(string username);
        Task<bool> DeleteUserAsync(string id);
    }
}
