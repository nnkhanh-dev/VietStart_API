using VietStart.API.Data;

namespace VietStart.API.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private ICategoryRepository _categoryRepository;
        private IStartUpRepository _startUpRepository;
        private ICommentRepository _commentRepository;
        private IReactRepository _reactRepository;
        private IShareRepository _shareRepository;
        private IStartUpMediaRepository _startUpMediaRepository;
        private IAppUserRepository _appUserRepository;
        private ITeamStartUpRepository _teamStartUpRepository;
        private IPositionRepository _positionRepository;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public ICategoryRepository Categories => _categoryRepository ??= new CategoryRepository(_context);
        public IStartUpRepository StartUps => _startUpRepository ??= new StartUpRepository(_context);
        public ICommentRepository Comments => _commentRepository ??= new CommentRepository(_context);
        public IReactRepository Reacts => _reactRepository ??= new ReactRepository(_context);
        public IShareRepository Shares => _shareRepository ??= new ShareRepository(_context);
        public IStartUpMediaRepository StartUpMedias => _startUpMediaRepository ??= new StartUpMediaRepository(_context);
        public IAppUserRepository Users => _appUserRepository ??= new AppUserRepository(_context);
        public ITeamStartUpRepository TeamStartUps => _teamStartUpRepository ??= new TeamStartUpRepository(_context);
        public IPositionRepository Positions => _positionRepository ??= new PositionRepository(_context);

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
