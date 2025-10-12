using VietStart.API.Entities.Domains;

namespace VietStart.API.Repositories
{
    public interface ITokenReposity
    {
        Task<string> CreateJWTToken(AppUser user, string role);
    }
}
