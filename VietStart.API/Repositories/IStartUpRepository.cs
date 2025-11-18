using VietStart.API.Entities.Domains;

namespace VietStart.API.Repositories
{
    public interface IStartUpRepository : IGenericRepository<StartUp>
    {
        Task<StartUp> GetStartUpWithDetailsAsync(int id);
        Task<IEnumerable<StartUp>> GetUserStartupsAsync(string userId);
        Task<IEnumerable<StartUp>> GetStartupsByCategoryAsync(int categoryId);
    }
}
