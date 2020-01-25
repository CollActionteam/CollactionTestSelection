using CollactionTestSelection.Models;
using CollactionTestSelection.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CollactionTestSelection.Controllers
{
    public sealed class HomeController : Controller
    {
        private readonly JiraOptions jiraOptions;
        private readonly GithubOptions githubOptions;
        private readonly DeployOptions awsOptions;
        private readonly NetiflyOptions netiflyOptions;
        private readonly SemaphoreSlim deploymentLock;
        private readonly ILogger<HomeController> logger;

        public HomeController(IOptions<GithubOptions> githubOptions, IOptions<DeployOptions> awsOptions, IOptions<JiraOptions> jiraOptions, IOptions<NetiflyOptions> netiflyOptions, SemaphoreSlim deploymentLock, ILogger<HomeController> logger)
        {
            this.jiraOptions = jiraOptions.Value;
            this.githubOptions = githubOptions.Value;
            this.awsOptions = awsOptions.Value;
            this.netiflyOptions = netiflyOptions.Value;
            this.deploymentLock = deploymentLock;
            this.logger = logger;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View(new IndexViewModel(pullRequests: await GetPullRequests()));
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Deploy(TagViewModel model)
        {
            if (!ModelState.IsValid)
                throw new InvalidOperationException("tag not specified or correct");

            return View(new DeployViewModel(tag: model.Tag, result: await RunDeploymentCommand(model.Tag)));
        }

        private async Task<IEnumerable<PullRequestModel>> GetPullRequests()
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CollAction/1.0");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

            try
            {
                string githubEndpoint = $"https://api.github.com/repos/{githubOptions.REPOSITORY_OWNER}/{githubOptions.REPOSITORY}/pulls?state=open";
                logger.LogInformation("sending github API request: {0}", githubEndpoint);
                using HttpResponseMessage message = await client.GetAsync(githubEndpoint, HttpCompletionOption.ResponseContentRead);
                using HttpContent content = message.Content;
                string messageContents = await message.Content.ReadAsStringAsync();

                if (message.StatusCode != HttpStatusCode.OK)
                    throw new HttpRequestException($"error response received from github: {message.StatusCode}, {messageContents}");

                logger.LogInformation("received github API response: {0}", messageContents);

                Regex pullRequestTag = new Regex($"{jiraOptions.PROJECT_KEY}-\\d+");

                IEnumerable<dynamic> relevantPullRequests = JArray.Parse(messageContents)
                             .Values<dynamic>()
                             .Where(dict => pullRequestTag.IsMatch((string)dict.head.label));

                return relevantPullRequests
                    .Select(dict =>
                    {
                        string tag = pullRequestTag.Match((string)dict.head.label).Value;
                        string branch = (string)dict.head["ref"];
                        string branchDomain = $"https://{WebUtility.UrlEncode(branch)}--{netiflyOptions.NETIFLY_BASE_URL}";
                        string jiraLink = $"https://{jiraOptions.JIRA_TEAM}.atlassian.net/browse/{tag}";
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

        private async Task<string> RunDeploymentCommand(string tag)
        {
            logger.LogInformation("waiting for deployment lock");
            await deploymentLock.WaitAsync();
            try
            {
                Dictionary<string, string> arguments = new Dictionary<string, string>()
                {
                    { "-k", awsOptions.AWS_ACCESS_KEY_ID },
                    { "-s", awsOptions.AWS_SECRET_ACCESS_KEY },
                    { "-r", awsOptions.AWS_DEFAULT_REGION },
                    { "-c", awsOptions.AWS_CLUSTER },
                    { "-n", awsOptions.AWS_SERVICE },
                    { "-i", $"{awsOptions.DOCKER_IMAGE}:{tag}" },
                    { "-D", $"{awsOptions.DESIRED_COUNT}" },
                    { "-M", $"{(int)Math.Round(100.0 * awsOptions.MAX_COUNT / awsOptions.DESIRED_COUNT)}" },
                    { "-t", $"{awsOptions.TIMEOUT}" }
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
                await Task.WhenAll(readStandardError, readStandardOut);

                process.WaitForExit();

                string standardOut = readStandardOut.Result;
                string standardError = readStandardError.Result;

                if (process.ExitCode != 0)
                    throw new InvalidOperationException($"deployment exited with error {process.ExitCode}: {standardError}, {standardOut}");

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