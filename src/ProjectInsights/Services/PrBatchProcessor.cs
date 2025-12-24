using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectInsights.Models;

namespace ProjectInsights.Services
{
    public class PrBatchProcessor
    {
        private readonly PrAnalysisService _prAnalysisService;
        private readonly GitHubService _gitHubService;

        public PrBatchProcessor(PrAnalysisService prAnalysisService, GitHubService gitHubService)
        {
            _prAnalysisService = prAnalysisService;
            _gitHubService = gitHubService;
        }

        public async Task<List<PrInfo>> ProcessPrsInBatchesAsync(DateTime startDate, DateTime endDate, string baseBranch)
        {
            var allPrInfos = new List<PrInfo>();
            DateTime currentDay = startDate;

            while (currentDay < endDate)
            {
                Console.WriteLine($"Processing PRs for batch: {currentDay:yyyy-MM-dd HH:mm:ss} UTC (base branch: {baseBranch})");
                var prInfos = await _prAnalysisService.AnalyzePullRequestsAsync(currentDay, baseBranch);

                Console.WriteLine($"Found {prInfos.Count} PRs in this batch");
                if (prInfos.Count > 0)
                {
                    Console.WriteLine($"  First PR: #{prInfos.First().Number} merged at {prInfos.First().MergedAt:yyyy-MM-dd HH:mm:ss} UTC");
                    Console.WriteLine($"  Last PR: #{prInfos.Last().Number} merged at {prInfos.Last().MergedAt:yyyy-MM-dd HH:mm:ss} UTC");
                }

                allPrInfos.AddRange(prInfos);

                // Check rate limit after each batch
                await _gitHubService.CheckRateLimitAsync();
                while (_gitHubService.GetRateLimitRemaining() <= 3000)
                {
                    Console.WriteLine("Rate limit too low. Pausing for 10 minutes...");
                    await Task.Delay(TimeSpan.FromMinutes(10));
                    await _gitHubService.CheckRateLimitAsync();
                }

                currentDay = currentDay.AddDays(1);
            }

            return allPrInfos;
        }
    }
}
