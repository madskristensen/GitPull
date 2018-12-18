using System;
using System.ComponentModel.Design;
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

            var cmdId = new CommandID(PackageGuids.guidGitPullPackageCmdSet, PackageIds.PullCommandId);
            var cmd = new MenuCommand((s, e) => Execute(commandService), cmdId)
            {
                Supported = false
            };

            commandService.AddCommand(cmd);
        }

        public static void Execute(OleMenuCommandService commandService)
        {
            var guid = new Guid("{57735D06-C920-4415-A2E0-7D6E6FBDFA99}");
            int id = 0x1033;
            var cmdId = new CommandID(guid, id);

            try
            {
                _ = commandService.GlobalInvoke(cmdId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex);
            }
        }
    }
}
