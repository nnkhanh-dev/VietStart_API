using Microsoft.EntityFrameworkCore;
using VietStart.API.Data;
using VietStart.API.Entities.Domains;

namespace VietStart.API.Repositories
{
    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Category>> GetCategoriesWithStartupsCountAsync()
        {
            return await _dbSet
                .Where(c => c.DeletedAt == null)
                .Include(c => c.StartUps.Where(s => s.DeletedAt == null))
                .ToListAsync();
        }
    }
}
