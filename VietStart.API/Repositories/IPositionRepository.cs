using VietStart.API.Entities.Domains;

namespace VietStart.API.Repositories
{
    public interface IPositionRepository : IGenericRepository<Position>
    {
        Task<Position> GetPositionByNameAsync(string name);
        Task<IEnumerable<Position>> SearchPositionsAsync(string keyword);
    }
}
