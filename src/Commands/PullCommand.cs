using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Process = System.Diagnostics.Process;
using Task = System.Threading.Tasks.Task;

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
                var dte = serviceProvider.GetService(typeof(DTE)) as DTE2;
                Assumes.Present(dte);

                var solutionDir = FindSolutionDirectory(dte);
                if (solutionDir == null)
                {
                    return;
                }

                var pane = new Lazy<IVsOutputWindowPane>(() =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    Window window = dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
                    window.Activate();
                    return package.GetOutputPane(Guid.NewGuid(), "Git Pull");
                });

                ExecuteAsync(dte, solutionDir, pane).FileAndForget("madskristensen/gitpull");
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
            }
        }

        private static async Task ExecuteAsync(DTE2 dte, string solutionDir, Lazy<IVsOutputWindowPane> pane)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var outputText = false;
            var progress = new Progress<string>(line =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                pane.Value.OutputString(line + Environment.NewLine);
                outputText = true;
            });

            await SyncRepositoryAsync(solutionDir, progress);

            if (!outputText)
            {
                dte.StatusBar.Text = "No branches require syncing";
            }
        }

        private static async Task SyncRepositoryAsync(string solutionDir, Progress<string> progress)
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

        private static async Task ReadAllAsync(StreamReader reader, IProgress<string> progress)
        {
            while (true)
            {
                var line = await reader.ReadLineAsync();
                if (line == null || line == "fatal: Not a git repository")
                {
                    break;
                }

                progress.Report(line);
            }
        }

        private static string FindSolutionDirectory(DTE2 dte)
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
