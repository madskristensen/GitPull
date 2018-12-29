using System;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace GitPull.Services
{
    public class GitPullUIService : IGitPullUIService
    {
        readonly Guid GitPaneGuid = new Guid("FBC10BF4-C9F8-4F0D-9CDE-69304226A68F");
        readonly Package package;
        readonly ITeamExplorerService teamExplorerService;
        readonly IHubService hubService;
        readonly DTE dte;

        public GitPullUIService(Package package, DTE dte,
            IHubService hubService, ITeamExplorerService teamExplorerService)
        {
            this.package = package;
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

            var pane = new Lazy<IVsOutputWindowPane>(() =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                Window window = dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
                window.Activate();
                return package.GetOutputPane(GitPaneGuid, "Source Control - Git");
            });

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
