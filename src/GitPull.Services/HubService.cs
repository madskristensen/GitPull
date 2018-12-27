using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GitPull.Services
{
    public class HubService : IHubService
    {
        public async Task SyncRepositoryAsync(string solutionDir, Progress<string> progress)
        {
            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string exeFile = Path.Combine(dir, "hub.exe");
            var startInfo = new ProcessStartInfo
            {
                FileName = exeFile,
                Arguments = "sync",
                WorkingDirectory = solutionDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            var process = Process.Start(startInfo);

            await Task.WhenAll(
                ReadAllAsync(process.StandardOutput, progress),
                ReadAllAsync(process.StandardError, progress));
        }

        static async Task ReadAllAsync(StreamReader reader, IProgress<string> progress)
        {
            while (true)
            {
                string line = await reader.ReadLineAsync();
                if (line == null || line == "fatal: Not a git repository")
                {
                    break;
                }

                progress.Report(line);
            }
        }
    }
}
