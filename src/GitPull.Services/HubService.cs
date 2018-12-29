using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GitPull.Services
{
    public class HubService : IHubService
    {
        public async Task SyncRepositoryAsync(string repositoryPath,
            Progress<string> outputProgress, Progress<string> statusProgress)
        {
            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string exeFile = Path.Combine(dir, "hub.exe");
            var startInfo = new ProcessStartInfo
            {
                FileName = exeFile,
                Arguments = "sync",
                WorkingDirectory = repositoryPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            var process = Process.Start(startInfo);

            await Task.WhenAll(
                ReadAllAsync(process.StandardOutput, outputProgress),
                ReadAllAsync(process.StandardError, statusProgress));
        }

        static async Task ReadAllAsync(StreamReader reader, IProgress<string> progress)
        {
            while (await reader.ReadLineAsync() is string line)
            {
                progress.Report(line);
            }
        }
    }
}
