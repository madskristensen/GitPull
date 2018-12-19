using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.ComponentModel.Design;
using EnvDTE;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;
using Process = System.Diagnostics.Process;

namespace GitPull
{
    internal sealed class PullCommand
    {
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Assumes.Present(commandService);

            var cmdId = new CommandID(PackageGuids.guidGitPullPackageCmdSet, PackageIds.PullCommandId);
            var cmd = new MenuCommand((s, e) => Execute(package), cmdId)
            {
                Supported = false
            };

            commandService.AddCommand(cmd);
        }

        public static void Execute(AsyncPackage package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var serviceProvider = package as IServiceProvider;
                Assumes.Present(serviceProvider);
                var dte = serviceProvider.GetService(typeof(DTE)) as DTE;
                Assumes.Present(dte);

                var solutionDir = FindSolutionDirectory(dte);
                if (solutionDir == null)
                {
                    return;
                }

                var pane = new Lazy<IVsOutputWindowPane>(() => package.GetOutputPane(Guid.NewGuid(), "Git Pull"));
                var progress = new Progress<string>(line =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    pane.Value.OutputStringThreadSafe(line + Environment.NewLine);
                });

                SyncRepositoryAsync(solutionDir, progress).FileAndForget("madskristensen/gitpull");
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
            }
        }

        static async Task SyncRepositoryAsync(string solutionDir, Progress<string> progress)
        {
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var exeFile = Path.Combine(dir, "hub.exe");
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
                var line = await reader.ReadLineAsync();
                if (line == null)
                {
                    break;
                }

                progress.Report(line);
            }
        }

        static string FindSolutionDirectory(DTE dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var path = dte.Solution.FileName;
            if (Directory.Exists(path))
            {
                return path;
            }

            if (File.Exists(path))
            {
                return Path.GetDirectoryName(path);
            }

            return null;
        }
    }
}
