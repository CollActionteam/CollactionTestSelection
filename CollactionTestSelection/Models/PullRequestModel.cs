using CollactionTestSelection.Options;

namespace CollactionTestSelection.Models
{
    public sealed class PullRequestModel
    {
        public PullRequestModel(string title, string tag, string githubLink, string jiraLink)
        {
            Title = title;
            Tag = tag;
            GithubLink = githubLink;
            JiraLink = jiraLink;
        }

        public string Title { get; }
        public string Tag { get; }
        public string GithubLink { get; }
        public string JiraLink { get; }
    }
}
