namespace CollactionTestSelection.Models
{
    public sealed class PullRequestModel
    {
        public PullRequestModel(string title, string githubLink, string jiraLink, bool hasDuplicates, string branchDomain, string dockerTag)
        {
            Title = title;
            GithubLink = githubLink;
            JiraLink = jiraLink;
            HasDuplicates = hasDuplicates;
            BranchDomain = branchDomain;
            DockerTag = dockerTag;
        }

        public string Title { get; }
        public string GithubLink { get; }
        public string JiraLink { get; }
        public bool HasDuplicates { get; }
        public string BranchDomain { get; }
        public string DockerTag { get; }

        public string? Warning
        {
            get
            {
                if (HasDuplicates)
                    return $"This pull request has a build tag that overlaps with other pull requests";
                else
                    return null;
            }
        }
    }
}
