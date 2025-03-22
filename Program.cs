using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Octokit;

class Program
{
    static async Task Main(string[] args)
    {
        Console.Write("Enter the GitHub username: ");
        string? username = Console.ReadLine();

        if (string.IsNullOrEmpty(username))
        {
            Console.WriteLine("Invalid username.");
            return;
        }

        Console.Write("Enter the folder path where repositories should be downloaded (e.g., C:\\Repos): ");
        string? downloadPath = Console.ReadLine();

        if (string.IsNullOrEmpty(downloadPath) || !Directory.Exists(downloadPath))
        {
            Console.WriteLine("Invalid download path.");
            return;
        }

        string userFolder = Path.Combine(downloadPath, username);
        if (!Directory.Exists(userFolder))
        {
            Directory.CreateDirectory(userFolder);
            Console.WriteLine($"Created directory: {userFolder}");
        }

        var client = new GitHubClient(new ProductHeaderValue("GitHubRepoDownloader"));

        try
        {
            var repositories = await client.Repository.GetAllForUser(username);

            if (repositories.Count == 0)
            {
                Console.WriteLine($"No repositories found for user {username}.");
                return;
            }

            Console.WriteLine($"Found {repositories.Count} repositories. Starting download...");

            foreach (var repo in repositories)
            {
                string repoUrl = repo.CloneUrl;
                string repoName = repo.Name;
                string repoPath = Path.Combine(userFolder, repoName);

                if (Directory.Exists(repoPath))
                {
                    Console.WriteLine($"Repository '{repoName}' already exists. Skipping...");
                    continue;
                }

                Console.WriteLine($"Cloning repository '{repoName}'...");
                await CloneRepositoryAsync(repoUrl, repoPath);
            }

            Console.WriteLine("Repositories download complete.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving repositories: {ex.Message}");
        }
    }

    static async Task CloneRepositoryAsync(string repoUrl, string repoPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"clone --recursive {repoUrl} \"{repoPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new Process
        {
            StartInfo = startInfo
        };

        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode == 0)
        {
            Console.WriteLine($"Successfully cloned to '{repoPath}'");
        }
        else
        {
            Console.WriteLine($"Error cloning repository: {error}");
        }
    }
}
