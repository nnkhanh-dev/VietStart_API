using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using VietStart.API.Data;
using VietStart.API.Entities.Domains;

namespace VietStart.API.Repositories
{
    public class StartUpRepository : GenericRepository<StartUp>, IStartUpRepository
    {
        public StartUpRepository(AppDbContext context) : base(context)
        {
        }

        public override async Task<(IEnumerable<StartUp> Data, int Total)> GetPaginatedAsync(
            int page,
            int pageSize,
            Expression<Func<StartUp, bool>> predicate = null,
            Func<IQueryable<StartUp>, IOrderedQueryable<StartUp>> orderBy = null)
        {
            IQueryable<StartUp> query = _dbSet
                .Include(s => s.AppUser)
                .Include(s => s.Category);

            if (predicate != null)
                query = query.Where(predicate);

            int total = await query.CountAsync();

            if (orderBy != null)
                query = orderBy(query);

            var data = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (data, total);
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
