using Microsoft.EntityFrameworkCore;
using VietStart.API.Data;
using VietStart.API.Entities.Domains;

namespace VietStart.API.Repositories
{
    public class PositionRepository : GenericRepository<Position>, IPositionRepository
    {
        public PositionRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Position> GetPositionByNameAsync(string name)
        {
            return await _dbSet
                .FirstOrDefaultAsync(p => p.Name.ToLower() == name.ToLower());
        }

        public async Task<IEnumerable<Position>> SearchPositionsAsync(string keyword)
        {
            return await _dbSet
                .Where(p => p.Name.Contains(keyword))
                .OrderBy(p => p.Name)
                .ToListAsync();
        }
    }
}
