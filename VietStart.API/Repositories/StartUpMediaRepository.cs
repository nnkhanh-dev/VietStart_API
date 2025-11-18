using Microsoft.EntityFrameworkCore;
using VietStart.API.Data;
using VietStart.API.Entities.Domains;

namespace VietStart.API.Repositories
{
    public class StartUpMediaRepository : GenericRepository<StartUpMedia>, IStartUpMediaRepository
    {
        public StartUpMediaRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<StartUpMedia>> GetMediasByStartupAsync(int startupId)
        {
            return await _dbSet
                .Where(m => m.StartUpId == startupId)
                .ToListAsync();
        }
    }
}
