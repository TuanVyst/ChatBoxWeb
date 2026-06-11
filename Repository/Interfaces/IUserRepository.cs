using BusinessObject.Entities;

namespace Repository.Interfaces
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(string id);
        Task<User> AddUserAsync(User user);
        Task<bool> DeleteUserAsync(string id);
    }
}
