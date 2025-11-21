using Microsoft.EntityFrameworkCore;
using VietStart.API.Data;
using VietStart.API.Entities.Domains;

namespace VietStart.API.Repositories
{
    public class TeamStartUpRepository : GenericRepository<TeamStartUp>, ITeamStartUpRepository
    {
        public TeamStartUpRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<TeamStartUp> GetTeamStartUpWithDetailsAsync(int id)
        {
            return await _dbSet
                .Include(t => t.User)
                .Include(t => t.StartUp)
                    .ThenInclude(s => s.Category)
                .Include(t => t.Position)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<IEnumerable<TeamStartUp>> GetTeamStartUpsByStartUpIdAsync(int startUpId)
        {
            return await _dbSet
                .Include(t => t.User)
                .Include(t => t.Position)
                .Where(t => t.StartUpId == startUpId)
                .ToListAsync();
        }

        public async Task<IEnumerable<TeamStartUp>> GetTeamStartUpsByUserIdAsync(string userId)
        {
            return await _dbSet
                .Include(t => t.StartUp)
                    .ThenInclude(s => s.Category)
                .Include(t => t.Position)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Id)
                .ToListAsync();
        }

        public async Task<IEnumerable<TeamStartUp>> GetTeamStartUpsByStatusAsync(string status)
        {
            return await _dbSet
                .Include(t => t.User)
                .Include(t => t.StartUp)
                .Include(t => t.Position)
                .Where(t => t.Status == status)
                .ToListAsync();
        }
    }
}
