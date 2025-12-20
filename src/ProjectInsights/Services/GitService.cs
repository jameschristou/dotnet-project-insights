using LibGit2Sharp;
using ProjectInsights.Models;

namespace ProjectInsights.Services;

public class GitService
{
    private readonly string _repoPath;

    public GitService(string repoPath)
    {
        _repoPath = repoPath;
    }

    /// <summary>
    /// Gets the files changed in a PR by analyzing the commit diff.
    /// Uses the merge commit SHA to compare with its first parent.
    /// </summary>
    public List<LocalPullRequestFile> GetPullRequestFiles(string commitSha)
    {
        using var repo = new Repository(_repoPath);
        
        var commit = repo.Lookup<Commit>(commitSha);
        if (commit == null)
        {
            throw new InvalidOperationException($"Commit {commitSha} not found");
        }

        // Get the first parent
        var parent = commit.Parents.FirstOrDefault();
        if (parent == null)
        {
            // This is the initial commit with no parent
            return new List<LocalPullRequestFile>();
        }

        // Compare the commit with its first parent to get the changes
        var compareOptions = new CompareOptions
        {
            Similarity = SimilarityOptions.Renames
        };

        var changes = repo.Diff.Compare<TreeChanges>(parent.Tree, commit.Tree, compareOptions);
        
        var files = new List<LocalPullRequestFile>();
        
        foreach (var change in changes)
        {
            // Get detailed patch to calculate additions and deletions
            var patchOptions = new CompareOptions
            {
                Similarity = SimilarityOptions.Renames,
                ContextLines = 0 // Minimize context to focus on actual changes
            };
            
            var patch = repo.Diff.Compare<Patch>(parent.Tree, commit.Tree, new[] { change.Path }, null, patchOptions);
            var filePatch = patch[change.Path];
            
            var status = change.Status switch
            {
                ChangeKind.Added => "added",
                ChangeKind.Deleted => "removed",
                ChangeKind.Modified => "modified",
                ChangeKind.Renamed => "renamed",
                _ => "modified"
            };

            var file = new LocalPullRequestFile
            {
                FileName = change.Path,
                Status = status,
                Additions = filePatch?.LinesAdded ?? 0,
                Deletions = filePatch?.LinesDeleted ?? 0,
                Changes = (filePatch?.LinesAdded ?? 0) + (filePatch?.LinesDeleted ?? 0)
            };

            files.Add(file);
        }

        return files;
    }
}
