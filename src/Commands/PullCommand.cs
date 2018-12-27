using System.ComponentModel.Design;
using GitPull.Services;
using Microsoft;
using Microsoft.VisualStudio.Shell;
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

            var service = await package.GetServiceAsync(typeof(IGitPullUIService)) as IGitPullUIService;
            Assumes.Present(service);

            var cmdId = new CommandID(PackageGuids.guidGitPullPackageCmdSet, PackageIds.PullCommandId);
            var cmd = new MenuCommand(
                (s, e) => service.SyncAndPullAsync().FileAndForget("madskristensen/gitpull"), cmdId)
            {
                Supported = false
            };

            commandService.AddCommand(cmd);
        }
    }
}
