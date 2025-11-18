using Microsoft.EntityFrameworkCore;
using VietStart.API.Data;
using VietStart.API.Entities.Domains;

namespace VietStart.API.Repositories
{
    public class AppUserRepository : GenericRepository<AppUser>, IAppUserRepository
    {
        public AppUserRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<AppUser> GetUserWithDetailsAsync(string userId)
        {
            return await _dbSet
                .Where(u => u.Id == userId && u.DeletedAt == null)
                .Include(u => u.StartUps.Where(s => s.DeletedAt == null))
                .Include(u => u.Comments.Where(c => c.DeletedAt == null))
                .Include(u => u.Shares.Where(s => s.DeletedAt == null))
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<AppUser>> SearchUsersAsync(string keyword)
        {
            return await _dbSet
                .Where(u => u.DeletedAt == null && 
                    (u.FullName.Contains(keyword) || u.Email.Contains(keyword)))
                .ToListAsync();
        }
    }
}
