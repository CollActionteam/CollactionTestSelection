using System.ComponentModel.DataAnnotations;

namespace CollactionTestSelection.Deployment
{
    public sealed class DeployOptions
    {
        [Required]
        public string AwsCluster { get; set; } = null!;

        [Required]
        public string AwsDefaultRegion { get; set; } = null!;

        [Required]
        public string AwsSecretAccessKeyID { get; set; } = null!;

        [Required]
        public string AwsSecretAccessKey { get; set; } = null!;

        [Required]
        public string AwsService { get; set; } = null!;

        [Required]
        public string DockerImage { get; set; } = null!;

        [Required]
        public string GithubRepositoryOwner { get; set; } = null!;

        [Required]
        public string GithubRepository { get; set; } = null!;

        [Required]
        public string JiraTeam { get; set; } = null!;

        [Required]
        public string JiraProjectKey { get; set; } = null!;

        [Required]
        public string NetiflyBaseUrl { get; set; } = null!;

        public int Timeout { get; set; } = 600;
    }
}
