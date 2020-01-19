namespace CollactionTestSelection.Models
{
    public sealed class PullRequestModel
    {
        public PullRequestModel(string title, string tag, string githubLink, string jiraLink, bool hasDuplicates, string branchDomain)
        {
            Title = title;
            Tag = tag;
            GithubLink = githubLink;
            JiraLink = jiraLink;
            HasDuplicates = hasDuplicates;
            BranchDomain = branchDomain;
        }

        public string Title { get; }
        public string Tag { get; }
        public string GithubLink { get; }
        public string JiraLink { get; }
        public bool HasDuplicates { get; }
        public string BranchDomain { get; }

        public string Warning
        {
            get
            {
                if (HasDuplicates)
                    return $"This pull request has a build tag that overlaps with other pull requests: {Tag}";
                else
                    return null;
            }
        }
    }
}
