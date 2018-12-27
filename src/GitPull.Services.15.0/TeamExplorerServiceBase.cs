using System;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Git.Controls.Commits;
using Microsoft.TeamFoundation.Git.CoreServices;
using Microsoft.TeamFoundation.Git.Provider;
using Microsoft.VisualStudio.TeamFoundation.Git.Extensibility;

namespace GitPull.Services
{
    // This needs to be compiled against TeamFoundation assemblies for the target Visual Studio version
    public abstract class TeamExplorerServiceBase : ITeamExplorerService
    {
        readonly IServiceProvider serviceProvider;

        public TeamExplorerServiceBase(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public async Task PullAsync(string repositoryPath)
        {
            Assumes.Present(repositoryPath);
            var service = serviceProvider.GetService(typeof(SccService)) as SccService;
            Assumes.Present(service);
            var teamExplorer = service.GetSccService<ITeamExplorer>();
            Assumes.NotNull(teamExplorer);
            var page = await NavigateToPageAsync(teamExplorer, new Guid(TeamExplorerPageIds.GitCommits));
            Assumes.NotNull(page);
            var gitCommitsPageView = page.PageContent as GitCommitsPageView;
            Assumes.NotNull(gitCommitsPageView);
            var gitCommitsPageViewModel = gitCommitsPageView.ViewModel as GitCommitsPageViewModel;
            Assumes.NotNull(gitCommitsPageViewModel);
            await gitCommitsPageViewModel.PullAsync(repositoryPath);
        }

        static async Task<ITeamExplorerPage> NavigateToPageAsync(ITeamExplorer teamExplorer, Guid pageId)
        {
            // Page sometimes returns null so we need to wait for CurrentPage to change
            var page = teamExplorer.NavigateToPage(pageId, null);
            while (page?.GetId() != pageId)
            {
                await Task.Delay(1000);
                page = teamExplorer.CurrentPage;
            }

            return page;
        }

        public string FindActiveRepositoryPath()
        {
            return
                serviceProvider.GetService(typeof(IGitExt)) is IGitExt gitExt &&
                gitExt.ActiveRepositories is var repos &&
                repos.Count > 0 ? repos[0].RepositoryPath : null;
        }
    }
}
