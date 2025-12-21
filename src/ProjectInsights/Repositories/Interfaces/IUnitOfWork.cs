namespace ProjectInsights.Repositories.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IAnalysisRunRepository AnalysisRuns { get; }
    IPullRequestRepository PullRequests { get; }
    IDailyStatsRepository DailyStats { get; }
    
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
