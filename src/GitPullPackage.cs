using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using GitPull.Services;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace GitPull
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [Guid(PackageGuids.guidGitPullPackageString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideService(typeof(IGitPullUIService), IsAsyncQueryable = true)]
    public sealed class GitPullPackage : AsyncPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            AddService(typeof(IHubService), CreateHubServiceAsync);
            AddService(typeof(ITeamExplorerService), CreateTeamExplorerServiceAsync);
            AddService(typeof(IGitPullUIService), CreateGitPullUIServiceAsync, true);

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await PullCommand.InitializeAsync(this);
        }

        async Task<object> CreateGitPullUIServiceAsync(IAsyncServiceContainer container, CancellationToken cancellationToken, Type serviceType)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = await GetServiceAsync(typeof(DTE)) as DTE;
            Assumes.Present(dte);
            var pane = new Lazy<IVsOutputWindowPane>(() =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                Window window = dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
                window.Activate();
                return GetOutputPane(Guid.NewGuid(), "Git Pull");
            });

            var hubService = await GetServiceAsync(typeof(IHubService)) as IHubService;
            Assumes.Present(hubService);
            var teamExplorerService = await GetServiceAsync(typeof(ITeamExplorerService)) as ITeamExplorerService;
            Assumes.Present(teamExplorerService);

            return new GitPullUIService(pane, dte, hubService, teamExplorerService);
        }

        Task<object> CreateHubServiceAsync(IAsyncServiceContainer container, CancellationToken cancellationToken, Type serviceType)
        {
            var service = new HubService();
            return Task.FromResult<object>(service);
        }

        Task<object> CreateTeamExplorerServiceAsync(IAsyncServiceContainer container, CancellationToken cancellationToken, Type serviceType)
        {
            var service = new TeamExplorerService(this);
            return Task.FromResult<object>(service);
        }
    }
}
