using BusinessObject.Dtos;
using BusinessObject.Entities;
using Repository.Interfaces;
using Service.Interfaces;

namespace Service.Implements
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;

        public UserService(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public async Task<IEnumerable<UserResponseDto>> GetAllUsersAsync()
        {
            var users = await _userRepo.GetAllUsersAsync();
            return users.Select(u => MapToDto(u));
        }

        public async Task<UserResponseDto?> GetUserByIdAsync(string id)
        {
            var user = await _userRepo.GetUserByIdAsync(id);
            return user == null ? null : MapToDto(user);
        }

        public async Task<UserResponseDto> LoginAsync(string username)
        {
            var existingUser = await _userRepo.GetUserByUsernameAsync(username);
            if (existingUser != null)
            {
                return MapToDto(existingUser);
            }

            var user = new User
            {
                Username = username
            };

            var created = await _userRepo.AddUserAsync(user);
            return MapToDto(created);
        }

        public async Task<bool> DeleteUserAsync(string id)
        {
            return await _userRepo.DeleteUserAsync(id);
        }

        private static UserResponseDto MapToDto(User user)
        {
            return new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
