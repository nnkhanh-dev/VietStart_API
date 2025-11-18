using Microsoft.EntityFrameworkCore;
using VietStart.API.Data;
using VietStart.API.Entities.Domains;

namespace VietStart.API.Repositories
{
    public class CommentRepository : GenericRepository<Comment>, ICommentRepository
    {
        public CommentRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Comment>> GetCommentsByStartupAsync(int startupId)
        {
            return await _dbSet
                .Where(c => c.StartUpId == startupId && c.DeletedAt == null && c.ParentCommentId == null)
                .Include(c => c.User)
                .Include(c => c.Replies.Where(r => r.DeletedAt == null))
                    .ThenInclude(r => r.User)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Comment> GetCommentWithRepliesAsync(int id)
        {
            return await _dbSet
                .Where(c => c.Id == id && c.DeletedAt == null)
                .Include(c => c.User)
                .Include(c => c.Replies.Where(r => r.DeletedAt == null))
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync();
        }
    }
}
