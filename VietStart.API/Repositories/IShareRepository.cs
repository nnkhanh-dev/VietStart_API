using VietStart.API.Entities.Domains;

namespace VietStart.API.Repositories
{
    public interface IShareRepository : IGenericRepository<Share>
    {
        Task<IEnumerable<Share>> GetSharesByStartupAsync(int startupId);
        Task<IEnumerable<Share>> GetSharesByUserAsync(string userId);
        Task<Share> GetShareAsync(string userId, int startupId);
    }
}
