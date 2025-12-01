namespace ProjectInsights.Services;

public class ProjectDiscoveryService
{
    private readonly string _repoPath;
    private readonly List<string> _projectGroups;
    private Dictionary<string, string> _projectToGroupMap = new();
    private Dictionary<string, string> _filePathToGroupMap = new();

    public ProjectDiscoveryService(string repoPath, List<string> projectGroups)
    {
        _repoPath = repoPath;
        _projectGroups = projectGroups;
    }

    public void DiscoverProjects()
    {
        Console.WriteLine("Discovering .csproj files...");
        
        var csprojFiles = Directory.GetFiles(_repoPath, "*.csproj", SearchOption.AllDirectories);
        Console.WriteLine($"Found {csprojFiles.Length} .csproj files");

        foreach (var csprojPath in csprojFiles)
        {
            var projectName = Path.GetFileNameWithoutExtension(csprojPath);
            var projectGroup = GetProjectGroup(projectName);
            
            _projectToGroupMap[projectName] = projectGroup;
            
            // Map the directory of this project to the project group
            var projectDir = Path.GetDirectoryName(csprojPath);
            if (!string.IsNullOrEmpty(projectDir))
            {
                _filePathToGroupMap[projectDir] = projectGroup;
            }
        }

        Console.WriteLine($"Mapped to {_projectToGroupMap.Values.Distinct().Count()} unique project groups");
    }

    public string GetProjectGroupForFile(string filePath)
    {
        // Convert to absolute path if needed
        var absolutePath = Path.IsPathRooted(filePath) 
            ? filePath 
            : Path.Combine(_repoPath, filePath);

        // Normalize path separators
        absolutePath = Path.GetFullPath(absolutePath);

        // Find the closest matching project directory
        var matchingDir = _filePathToGroupMap.Keys
            .Where(dir => absolutePath.StartsWith(dir, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(dir => dir.Length)
            .FirstOrDefault();

        if (matchingDir != null)
        {
            return _filePathToGroupMap[matchingDir];
        }

        // If no match found, try to extract project name from path
        var segments = absolutePath.Replace(_repoPath, "").Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        foreach (var segment in segments)
        {
            if (_projectToGroupMap.ContainsKey(segment))
            {
                return _projectToGroupMap[segment];
            }
        }

        return "Unmatched";
    }

    public List<string> GetAllProjectGroups()
    {
        var groups = _projectToGroupMap.Values.Distinct().OrderBy(g => g).ToList();
        return groups;
    }

    private string GetProjectGroup(string projectName)
    {
        // Use longest-prefix-first matching (list is already sorted by length descending)
        foreach (var group in _projectGroups)
        {
            if (projectName.StartsWith(group, StringComparison.OrdinalIgnoreCase))
            {
                return group;
            }
        }

        // If no match, project becomes its own group
        return projectName;
    }
}
