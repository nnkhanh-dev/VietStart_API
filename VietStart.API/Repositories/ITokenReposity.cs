using VietStart.API.Entities.Domains;

namespace VietStart.API.Repositories
{
    public interface ITokenReposity
    {
        Task<string> CreateJWTToken(AppUser user, string role);
        Task<RefreshToken> GenerateRefreshTokenAsync(AppUser user);
        Task SaveRefreshTokenAsync(RefreshToken refreshToken);
        Task<RefreshToken?> GetRefreshTokenAsync(string refreshToken);
        Task RevokeRefreshTokenAsync(string refreshToken);
    }
}
