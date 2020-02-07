using CollactionTestSelection.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CollactionTestSelection.Deployment
{
    public class DeploymentService : IDeploymentService
    {
        private static readonly SemaphoreSlim deploymentLock = new SemaphoreSlim(1, 1);
        private readonly DeployOptions deployOptions;
        private readonly ILogger<DeploymentService> logger;

        public DeploymentService(IOptions<DeployOptions> deployOptions, ILogger<DeploymentService> logger)
        {
            this.deployOptions = deployOptions.Value;
            this.logger = logger;
        }

        public async Task<IEnumerable<PullRequestModel>> GetPullRequests()
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CollAction/1.0");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

            try
            {
                string githubEndpoint = $"https://api.github.com/repos/{deployOptions.GithubRepositoryOwner}/{deployOptions.GithubRepository}/pulls?state=open";
                logger.LogInformation("sending github API request: {0}", githubEndpoint);
                using HttpResponseMessage message = await client.GetAsync(githubEndpoint, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
                using HttpContent content = message.Content;
                string messageContents = await message.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (message.StatusCode != HttpStatusCode.OK)
                    throw new HttpRequestException($"error response received from github: {message.StatusCode}, {messageContents}");

                logger.LogInformation("received github API response: {0}", messageContents);

                Regex pullRequestTag = new Regex($"{deployOptions.JiraProjectKey}-\\d+");

                IEnumerable<dynamic> relevantPullRequests = JArray.Parse(messageContents)
                             .Values<dynamic>()
                             .Where(dict => pullRequestTag.IsMatch((string)dict.head.label));

                return relevantPullRequests
                    .Select(dict =>
                    {
                        string tag = pullRequestTag.Match((string)dict.head.label).Value;
                        string branch = (string)dict.head["ref"];
                        string branchDomain = $"https://{WebUtility.UrlEncode(branch)}--{deployOptions.NetiflyBaseUrl}";
                        string jiraLink = $"https://{deployOptions.JiraTeam}.atlassian.net/browse/{tag}";
                        bool hasDuplicates = relevantPullRequests.Any(pr => pullRequestTag.Match((string)pr.head.label).Value == tag && pr.id != dict.id);
                        return new PullRequestModel(
                            title: (string)dict.title,
                            tag: tag,
                            githubLink: (string)dict.html_url,
                            jiraLink: jiraLink,
                            branchDomain: branchDomain,
                            hasDuplicates: hasDuplicates);
                    })
                    .ToList();
            }
            catch (HttpRequestException e)
            {
                throw new InvalidOperationException("unable to retrieve the github pull requests", e);
            }
        }

        public async Task<string> RunDeploymentCommand(string tag)
        {
            logger.LogInformation("waiting for deployment lock");
            await deploymentLock.WaitAsync().ConfigureAwait(false);
            try
            {
                logger.LogInformation("Creating aws configuration directory");
                DirectoryInfo info = Directory.CreateDirectory("/root/.aws/");

                logger.LogInformation("Creating aws configuration");
                File.WriteAllText("/root/.aws/config", $@"[default]
region = {deployOptions.AwsDefaultRegion}");
                File.WriteAllText("/root/.aws/credentials", $@"[default]
aws_access_key_id = {deployOptions.AwsSecretAccessKeyID}
aws_secret_access_key = {deployOptions.AwsSecretAccessKey}");

                Dictionary<string, string> arguments = new Dictionary<string, string>()
                {
                    { "-k", deployOptions.AwsSecretAccessKeyID },
                    { "-s", deployOptions.AwsSecretAccessKey },
                    { "-r", deployOptions.AwsDefaultRegion },
                    { "-c", deployOptions.AwsCluster },
                    { "-n", deployOptions.AwsService },
                    { "-i", $"{deployOptions.DockerImage}:{tag}" },
                    { "-D", $"1" }, // desired count
                    { "-M", $"200" }, // max count * 100 / desired count
                    { "-t", $"{deployOptions.Timeout}" }
                };

                string argumentString = $"/app/ecs-deploy {string.Join(" ", arguments.Select(arg => $"{arg.Key} {arg.Value}"))}";

                logger.LogInformation("starting deployment: {0}", argumentString);

                using Process process = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = "/bin/bash",
                        Arguments = argumentString,
                        CreateNoWindow = true,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        ErrorDialog = false,
                        UseShellExecute = false
                    }
                };

                process.Start();
                Task<string> readStandardOut = process.StandardOutput.ReadToEndAsync();
                Task<string> readStandardError = process.StandardError.ReadToEndAsync();
                await Task.WhenAll(readStandardError, readStandardOut).ConfigureAwait(false);

                process.WaitForExit();

                string standardOut = readStandardOut.Result;
                string standardError = readStandardError.Result;

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"deployment exited with error {process.ExitCode}: {standardError}, {standardOut}");
                }

                logger.LogInformation("finished deployment: {0}, {1}", standardOut, standardError);
                return standardOut;
            }
            finally
            {
                deploymentLock.Release();
            }
        }
    }
}
