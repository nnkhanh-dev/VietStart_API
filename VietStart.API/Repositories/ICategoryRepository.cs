using VietStart.API.Entities.Domains;

namespace VietStart.API.Repositories
{
    public interface ICategoryRepository : IGenericRepository<Category>
    {
        Task<IEnumerable<Category>> GetCategoriesWithStartupsCountAsync();
    }
}
