namespace Shared;

using System.Diagnostics;

public static class GitInfo
{
    public static (string commit, string branch) GetGitInfo()
    {
        var commit = ExecuteGitCommand("rev-parse HEAD");
        var branch = Environment.GetEnvironmentVariable("GIT_BRANCH") ?? ExecuteGitCommand("rev-parse --abbrev-ref HEAD");
        
        if (!string.IsNullOrEmpty(commit) && !string.IsNullOrEmpty(branch))
        {
            return (commit, branch);
        }
        
        var envCommit = Environment.GetEnvironmentVariable("GIT_COMMIT") ?? "unknown";
        var envBranch = Environment.GetEnvironmentVariable("GIT_BRANCH") ?? "unknown";
        
        if (envBranch.StartsWith("origin/"))
        {
            envBranch = envBranch.Substring("origin/".Length);
        }

        return (envCommit, envBranch);
    }

    private static string? ExecuteGitCommand(string arguments)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            
            return process.ExitCode == 0 ? output : null;
        }
        catch
        {
            return null;
        }
    }
}