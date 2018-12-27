using System.Threading.Tasks;

namespace GitPull.Services
{
    public interface IGitPullUIService
    {
        Task SyncAndPullAsync();
    }
}