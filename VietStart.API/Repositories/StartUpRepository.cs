using Microsoft.EntityFrameworkCore;
using VietStart.API.Data;
using VietStart.API.Entities.Domains;

namespace VietStart.API.Repositories
{
    public class StartUpRepository : GenericRepository<StartUp>, IStartUpRepository
    {
        public StartUpRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<StartUp> GetStartUpWithDetailsAsync(int id)
        {
            return await _dbSet
                .Where(s => s.Id == id && s.DeletedAt == null)
                .Include(s => s.AppUser)
                .Include(s => s.Category)
                .Include(s => s.Comments.Where(c => c.DeletedAt == null && c.ParentCommentId == null))
                    .ThenInclude(c => c.Replies.Where(r => r.DeletedAt == null))
                .Include(s => s.Shares.Where(sh => sh.DeletedAt == null))
                .Include(s => s.Reacts.Where(r => r.StartUpId == id))
                .Include(s => s.StartUpMedias)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<StartUp>> GetUserStartupsAsync(string userId)
        {
            return await _dbSet
                .Where(s => s.UserId == userId && s.DeletedAt == null)
                .Include(s => s.AppUser)
                .Include(s => s.Category)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<StartUp>> GetStartupsByCategoryAsync(int categoryId)
        {
            return await _dbSet
                .Where(s => s.CategoryId == categoryId && s.DeletedAt == null)
                .Include(s => s.AppUser)
                .Include(s => s.Category)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }
    }
}
