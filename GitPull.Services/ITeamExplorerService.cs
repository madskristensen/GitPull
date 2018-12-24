using System.Threading.Tasks;

namespace GitPull.Services
{
    public interface ITeamExplorerService
    {
        Task PullAsync();

        string FindActiveRepositoryPath();
    }
}
