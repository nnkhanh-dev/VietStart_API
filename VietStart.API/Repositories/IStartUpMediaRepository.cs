using VietStart.API.Entities.Domains;

namespace VietStart.API.Repositories
{
    public interface IStartUpMediaRepository : IGenericRepository<StartUpMedia>
    {
        Task<IEnumerable<StartUpMedia>> GetMediasByStartupAsync(int startupId);
    }
}
