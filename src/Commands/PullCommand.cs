using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using EnvDTE;
using GitPull.Services;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
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
                var dte = serviceProvider.GetService(typeof(DTE)) as DTE;
                Assumes.Present(dte);
                var hubService = new HubService();
                var teamExplorerService = new TeamExplorerService(serviceProvider);

                var repositoryPath = teamExplorerService.FindActiveRepositoryPath();
                if (repositoryPath == null)
                {
                    dte.StatusBar.Text = "Not a git repository";
                    return;
                }

                var pane = new Lazy<IVsOutputWindowPane>(() =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    Window window = dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
                    window.Activate();
                    return package.GetOutputPane(Guid.NewGuid(), "Git Pull");
                });

                ExecuteAsync(dte, repositoryPath, pane, hubService, teamExplorerService).FileAndForget("madskristensen/gitpull");
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
            }
        }

        static async Task ExecuteAsync(DTE dte, string solutionDir, Lazy<IVsOutputWindowPane> pane,
            IHubService hubService, ITeamExplorerService teamExplorerService)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            bool outputText = false;
            var progress = new Progress<string>(line =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                pane.Value.OutputString(line + Environment.NewLine);
                outputText = true;
            });

            await hubService.SyncRepositoryAsync(solutionDir, progress);

            if (!outputText)
            {
                dte.StatusBar.Text = "No branches require syncing";
            }
            else
            {
                await teamExplorerService.PullAsync();
            }
        }
    }
}
