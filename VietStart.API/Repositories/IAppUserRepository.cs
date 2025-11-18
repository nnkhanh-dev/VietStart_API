using VietStart.API.Entities.Domains;

namespace VietStart.API.Repositories
{
    public interface IAppUserRepository : IGenericRepository<AppUser>
    {
        Task<AppUser> GetUserWithDetailsAsync(string userId);
        Task<IEnumerable<AppUser>> SearchUsersAsync(string keyword);
    }
}
