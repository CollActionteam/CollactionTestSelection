using System.Collections.Generic;

namespace CollactionTestSelection.Models
{
    public sealed class IndexViewModel
    {
        public IndexViewModel(IEnumerable<PullRequestModel> pullRequests)
        {
            PullRequests = pullRequests;
        }

        public IEnumerable<PullRequestModel> PullRequests { get; }
    }
}
