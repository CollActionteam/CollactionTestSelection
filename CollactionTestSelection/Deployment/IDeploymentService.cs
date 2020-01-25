using CollactionTestSelection.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CollactionTestSelection.Deployment
{
    public interface IDeploymentService
    {
        Task<IEnumerable<PullRequestModel>> GetPullRequests();

        Task<string> RunDeploymentCommand(string tag);
    }
}