using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GitPull.Services;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace GitPull
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [Guid(PackageGuids.guidGitPullPackageString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class GitPullPackage : AsyncPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            AddService(typeof(IHubService), CreateHubServiceAsync);
            AddService(typeof(ITeamExplorerService), CreateTeamExplorerServiceAsync);

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await PullCommand.InitializeAsync(this);
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
