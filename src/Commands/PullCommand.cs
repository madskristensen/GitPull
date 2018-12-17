using System;
using System.ComponentModel.Design;
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
            var statusBar = await package.GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar;
            Assumes.Present(commandService);

            var cmdId = new CommandID(PackageGuids.guidGitPullPackageCmdSet, PackageIds.PullCommandId);
            var cmd = new MenuCommand((s, e) => Execute(commandService, statusBar), cmdId)
            {
                Supported = false
            };

            commandService.AddCommand(cmd);
        }

        private static void Execute(OleMenuCommandService commandService, IVsStatusbar statusbar)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var guid = new Guid("{57735D06-C920-4415-A2E0-7D6E6FBDFA99}");
            int id = 0x1033;
            var cmdId = new CommandID(guid, id);

            statusbar.FreezeOutput(0);

            try
            {
                _ = commandService.GlobalInvoke(cmdId);
                statusbar.SetText("Git Pull invoked");
            }
            catch (Exception)
            {
                statusbar.SetText("Git pull failed");
            }
            finally
            {
                statusbar.FreezeOutput(1);
            }
        }
    }
}
