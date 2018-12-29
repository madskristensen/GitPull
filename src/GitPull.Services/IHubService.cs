using System;
using System.Threading.Tasks;

namespace GitPull.Services
{
    public interface IHubService
    {
        Task SyncRepositoryAsync(string solutionDir, Progress<string> outputProgress, Progress<string> statusProgress);
    }
}