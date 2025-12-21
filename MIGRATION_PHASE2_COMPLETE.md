# PostgreSQL Migration - Phase 2 Complete ?

## What We've Accomplished

### 1. Created Repository Interfaces
- ? `IAnalysisRunRepository` - Manage analysis run records
- ? `IPullRequestRepository` - Bulk insert PRs, files, and projects
- ? `IDailyStatsRepository` - UPSERT daily statistics with merge logic
- ? `IUnitOfWork` - Transaction management and repository access

### 2. Implemented Repositories
- ? `AnalysisRunRepository` - CRUD operations for analysis runs
- ? `PullRequestRepository` - Bulk insert operations for PRs and related data
- ? `DailyStatsRepository` - **Smart UPSERT logic** that:
  - Fetches existing records for overlapping days
  - Merges new stats with existing stats (adds PR counts, LOC, files)
  - Updates `updated_at` timestamp
  - Inserts new records where none exist
- ? `UnitOfWork` - Implements transaction support and repository coordination

### 3. Created DatabaseExportService
- ? Replaces `GoogleSheetsService` with database persistence
- ? `ExportDataAsync()` - Main entry point with transaction support
- ? `CreateAnalysisRunAsync()` - Creates analysis_run record
- ? `SavePullRequestsAsync()` - Saves PRs, files (with project info), and projects
- ? `CalculateAndUpsertDailyStatsAsync()` - Aggregates and upserts daily stats

## Key Features Implemented

### UPSERT Logic for Daily Stats
The `DailyStatsRepository` implements intelligent merge logic:

**For `daily_project_stats`:**
```csharp
// If record exists for (day, project_name):
existingRecord.PrCount += newStat.PrCount;
existingRecord.TotalLinesChanged += newStat.TotalLinesChanged;
existingRecord.FilesModified += newStat.FilesModified;
existingRecord.FilesAdded += newStat.FilesAdded;
existingRecord.UpdatedAt = now;

// Otherwise: Insert new record
```

**For `daily_team_project_stats`:**
```csharp
// If record exists for (day, project_name, team_name):
existingRecord.PrCount += newStat.PrCount;
existingRecord.UpdatedAt = now;

// Otherwise: Insert new record
```

### Transaction Support
All database operations are wrapped in transactions:
- `BeginTransactionAsync()` - Start transaction
- `CommitTransactionAsync()` - Commit on success
- `RollbackTransactionAsync()` - Rollback on error

### Data Flow
```
PrInfo (from analysis)
    ?
DatabaseExportService.ExportDataAsync()
    ?
1. Create AnalysisRun
2. Save PullRequests + PrFiles + PrProjects
3. Calculate DailyProjectStats + DailyTeamProjectStats
4. Upsert daily stats (merge with existing)
    ?
PostgreSQL Database
```

## Repository Pattern Benefits

? **Separation of Concerns** - Data access logic isolated from business logic
? **Testability** - Easy to mock repositories for unit tests
? **Maintainability** - Single place to change data access code
? **Database Portability** - Easy to swap PostgreSQL for SQL Server
? **Transaction Management** - Consistent transaction handling via UnitOfWork

## Files Created

### Interfaces
- `src/ProjectInsights/Repositories/Interfaces/IAnalysisRunRepository.cs`
- `src/ProjectInsights/Repositories/Interfaces/IPullRequestRepository.cs`
- `src/ProjectInsights/Repositories/Interfaces/IDailyStatsRepository.cs`
- `src/ProjectInsights/Repositories/Interfaces/IUnitOfWork.cs`

### Implementations
- `src/ProjectInsights/Repositories/Implementations/AnalysisRunRepository.cs`
- `src/ProjectInsights/Repositories/Implementations/PullRequestRepository.cs`
- `src/ProjectInsights/Repositories/Implementations/DailyStatsRepository.cs`
- `src/ProjectInsights/Repositories/Implementations/UnitOfWork.cs`

### Services
- `src/ProjectInsights/Services/DatabaseExportService.cs`

## Next Steps - Phase 3: Update Program.cs

1. Update `Program.cs` to:
   - Add `--postgres-connection` argument
   - Remove `--google-creds` and `--spreadsheet-id` arguments
   - Instantiate `DbContext` and `UnitOfWork`
   - Replace `GoogleSheetsService` with `DatabaseExportService`
   
2. Test the complete flow:
   - Apply migration to PostgreSQL
   - Run the application
   - Verify data in database
   - Run again with overlapping day to verify UPSERT logic

## Example Usage Pattern

```csharp
// Create DbContext
var optionsBuilder = new DbContextOptionsBuilder<ProjectInsightsDbContext>();
optionsBuilder.UseNpgsql(config.PostgresConnectionString);
var dbContext = new ProjectInsightsDbContext(optionsBuilder.Options);

// Create UnitOfWork
using var unitOfWork = new UnitOfWork(dbContext);

// Create DatabaseExportService
var dbExportService = new DatabaseExportService(unitOfWork, projectDiscovery);

// Export data
await dbExportService.ExportDataAsync(
    config.GitHubOwner,
    config.GitHubRepo,
    config.StartDate,
    config.EndDate,
    config.GitHubBaseBranch,
    prInfos);
```

## Smart Features

### Project Name Extraction
The service intelligently extracts project names from file paths:
- Looks for directory structure patterns
- Falls back to "Unknown" if pattern not found
- Can be customized based on repository structure

### Daily Aggregation
Stats are grouped by:
- **Day** - Date portion of PR's `merged_at`
- **Project** - Extracted from file paths
- **Team** - From team configuration

### Overlapping Day Handling
When runs overlap on the same day:
1. Existing stats are fetched from database
2. New stats are added to existing stats
3. Single UPDATE query per record
4. Maintains data consistency
