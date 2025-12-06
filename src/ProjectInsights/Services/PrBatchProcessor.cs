using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectInsights.Models;
using Octokit;

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

        public async Task<List<PrInfo>> ProcessPrsInBatchesAsync(DateTime startDate, DateTime endDate)
        {
            var allPrInfos = new List<PrInfo>();
            DateTime batchStart = startDate;
            DateTime batchEnd = batchStart.AddDays(1);

            while (batchStart < endDate)
            {
                if (batchEnd > endDate)
                    batchEnd = endDate;

                Console.WriteLine($"Processing PRs for batch: {batchStart:yyyy-MM-dd} to {batchEnd:yyyy-MM-dd}");
                var prInfos = await _prAnalysisService.AnalyzePullRequestsAsync(batchStart, batchEnd);
                allPrInfos.AddRange(prInfos);

                // Check rate limit after each batch
                await _gitHubService.CheckRateLimitAsync();
                while (_gitHubService.GetRateLimitRemaining() <= 3000)
                {
                    Console.WriteLine("Rate limit too low. Pausing for 10 minutes...");
                    await Task.Delay(TimeSpan.FromMinutes(10));
                    await _gitHubService.CheckRateLimitAsync();
                }

                batchStart = batchEnd;
                batchEnd = batchStart.AddDays(1);
            }

            return allPrInfos;
        }
    }
}
