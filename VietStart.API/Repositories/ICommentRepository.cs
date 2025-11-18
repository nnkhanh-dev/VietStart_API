using VietStart.API.Entities.Domains;

namespace VietStart.API.Repositories
{
    public interface ICommentRepository : IGenericRepository<Comment>
    {
        Task<IEnumerable<Comment>> GetCommentsByStartupAsync(int startupId);
        Task<Comment> GetCommentWithRepliesAsync(int id);
    }
}
