using System;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace GitPull.Services
{
    public class GitPullUIService : IGitPullUIService
    {
        readonly Lazy<IVsOutputWindowPane> pane;
        readonly ITeamExplorerService teamExplorerService;
        readonly IHubService hubService;
        readonly DTE dte;

        public GitPullUIService(Lazy<IVsOutputWindowPane> pane, DTE dte,
            IHubService hubService, ITeamExplorerService teamExplorerService)
        {
            this.pane = pane;
            this.teamExplorerService = teamExplorerService;
            this.hubService = hubService;
            this.dte = dte;
        }

        public async Task SyncAndPullAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var repositoryPath = teamExplorerService.FindActiveRepositoryPath();
            if (repositoryPath == null)
            {
                dte.StatusBar.Text = "Not a git repository";
                return;
            }

            if (pane.IsValueCreated)
            {
                // If pane already created then clear it
                pane.Value.Clear();
            }

            bool outputText = false;
            var progress = new Progress<string>(line =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                pane.Value.OutputString(line + Environment.NewLine);
                outputText = true;
            });

            await hubService.SyncRepositoryAsync(repositoryPath, progress);

            if (!outputText)
            {
                dte.StatusBar.Text = "No branches require syncing";
            }
            else
            {
                await teamExplorerService.PullAsync(repositoryPath);
            }
        }
    }
}
