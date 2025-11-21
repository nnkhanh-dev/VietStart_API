namespace VietStart.API.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        ICategoryRepository Categories { get; }
        IStartUpRepository StartUps { get; }
        ICommentRepository Comments { get; }
        IReactRepository Reacts { get; }
        IShareRepository Shares { get; }
        IStartUpMediaRepository StartUpMedias { get; }
        IAppUserRepository Users { get; }
        ITeamStartUpRepository TeamStartUps { get; }
        IPositionRepository Positions { get; }
        
        Task<int> SaveChangesAsync();
    }
}
