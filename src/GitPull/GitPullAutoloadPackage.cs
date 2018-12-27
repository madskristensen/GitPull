using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GitPull.Services;
using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using ShellEvents = Microsoft.VisualStudio.Shell.Events;
using Task = System.Threading.Tasks.Task;

namespace GitPull
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid("02222c3a-8662-44bc-aa3a-a4bbd12d8d70")]
    [ProvideOptionPage(typeof(Options.Page), "Source Control", Vsix.Name, 101, 102, true, new string[0], ProvidesLocalizedCategoryName = false)]
    [ProvideProfile(typeof(Options.Page), "Source Control", Vsix.Name, 101, 102, isToolsOptionPage: true)]
    [ProvideAutoLoad(PackageGuids.guidGitPullAutoloadString, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideUIContextRule(PackageGuids.guidGitPullAutoloadString,
        name: "Auto load",
        expression: "setting & solutionload",
        termNames: new[] { "setting", "solutionload" },
        termValues: new[] { @"UserSettingsStoreQuery:GitPull.GeneralOptions\PullOnSolutionOpen", VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string })]
    public sealed class GitPullAutoloadPackage : AsyncPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            bool isSolutionLoaded = await IsSolutionLoadedAsync();

            if (isSolutionLoaded)
            {
                HandleOpenSolutionAsync().FileAndForget("madskristensen/gitpull");
            }

            // Listen for subsequent solution events
            ShellEvents.SolutionEvents.OnAfterBackgroundSolutionLoadComplete += (s, e) =>
             {
                 HandleOpenSolutionAsync().FileAndForget("madskristensen/gitpull");
             };
        }

        private async Task<bool> IsSolutionLoadedAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var solService = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            Assumes.Present(solService);

            ErrorHandler.ThrowOnFailure(solService.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out object value));

            return value is bool isSolOpen && isSolOpen;
        }

        private async Task HandleOpenSolutionAsync(object sender = null, EventArgs e = null)
        {
            try
            {
                Options options = await Options.GetLiveInstanceAsync();

                if (!options.PullOnSolutionOpen)
                {
                    return;
                }

                await JoinableTaskFactory.SwitchToMainThreadAsync();

                var service = await GetServiceAsync(typeof(IGitPullUIService)) as IGitPullUIService;
                Assumes.Present(service);
                service.SyncAndPullAsync().FileAndForget("madskristensen/gitpull");
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
            }
        }
    }
}
