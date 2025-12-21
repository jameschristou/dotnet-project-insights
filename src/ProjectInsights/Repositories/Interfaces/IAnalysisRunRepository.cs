using ProjectInsights.Data.Entities;

namespace ProjectInsights.Repositories.Interfaces;

public interface IAnalysisRunRepository
{
    Task<AnalysisRun> CreateAsync(AnalysisRun analysisRun);
    Task<AnalysisRun?> GetLatestAsync(string owner, string repo);
    Task<AnalysisRun?> GetByIdAsync(long id);
}
