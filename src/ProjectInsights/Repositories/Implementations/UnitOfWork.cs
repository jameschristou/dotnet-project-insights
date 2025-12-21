using Microsoft.EntityFrameworkCore.Storage;
using ProjectInsights.Data;
using ProjectInsights.Repositories.Interfaces;

namespace ProjectInsights.Repositories.Implementations;

public class UnitOfWork : IUnitOfWork
{
    private readonly ProjectInsightsDbContext _context;
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    public UnitOfWork(ProjectInsightsDbContext context)
    {
        _context = context;
        AnalysisRuns = new AnalysisRunRepository(context);
        PullRequests = new PullRequestRepository(context);
        DailyStats = new DailyStatsRepository(context);
    }

    public IAnalysisRunRepository AnalysisRuns { get; }
    public IPullRequestRepository PullRequests { get; }
    public IDailyStatsRepository DailyStats { get; }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _transaction?.Dispose();
                _context.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
