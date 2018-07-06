using CollactionTestSelection.Models;
using CollactionTestSelection.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CollactionTestSelection.Controllers
{
    public sealed class DeploymentController : Controller
    {
        private readonly JiraOptions _jiraOptions;
        private readonly GithubOptions _githubOptions;
        private readonly DeployOptions _awsOptions;
        private readonly SemaphoreSlim _deploymentLock;
        private readonly ILogger<DeploymentController> _logger;

        public DeploymentController(IOptions<GithubOptions> githubOptions, IOptions<DeployOptions> awsOptions, IOptions<JiraOptions> jiraOptions, SemaphoreSlim deploymentLock, ILogger<DeploymentController> logger)
        {
            _jiraOptions = jiraOptions.Value;
            _githubOptions = githubOptions.Value;
            _awsOptions = awsOptions.Value;
            _deploymentLock = deploymentLock;
            _logger = logger;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View(new IndexViewModel(pullRequests: await GetPullRequests()));
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Deploy([Required] [RegularExpression(@"^[A-Z]+-\d+$")] string tag)
        {
            if (!ModelState.IsValid)
                throw new InvalidOperationException("tag not specified or correct");

            return View(new DeployViewModel(tag: tag, result: await RunDeploymentCommand(tag)));
        }

        [HttpGet]
        public IActionResult HealthCheck()
        {
            return Content("OK");
        }

        private async Task<IEnumerable<PullRequestModel>> GetPullRequests()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("CollAction/1.0");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

                try
                {
                    string githubEndpoint = $"https://api.github.com/repos/{_githubOptions.REPOSITORY_OWNER}/{_githubOptions.REPOSITORY}/pulls?state=open";
                    _logger.LogInformation("sending github API request: {0}", githubEndpoint);
                    using (HttpResponseMessage message = await client.GetAsync(githubEndpoint, HttpCompletionOption.ResponseContentRead))
                    {
                        using (HttpContent content = message.Content)
                        {
                            string messageContents = await message.Content.ReadAsStringAsync();

                            if (message.StatusCode != HttpStatusCode.OK)
                                throw new HttpRequestException($"error response received from github: {message.StatusCode}, {messageContents}");

                            _logger.LogInformation("received github API response: {0}", messageContents);

                            Regex pullRequestTag = new Regex($"{_jiraOptions.PROJECT_KEY}-\\d+");

                            return JArray.Parse(messageContents)
                                         .Values<dynamic>()
                                         .Where(dict => pullRequestTag.IsMatch((string)dict.head.label))
                                         .Select(dict => 
                                             new PullRequestModel(
                                                 title: (string)dict.title, 
                                                 tag: pullRequestTag.Match((string)dict.head.label).Value, 
                                                 githubLink: (string)dict.html_url, 
                                                 jiraLink: $"https://{_jiraOptions.JIRA_TEAM}.atlassian.net/browse/{pullRequestTag.Match((string)dict.head.label).Value}"))
                                         .ToList();
                        }
                    }
                }
                catch (HttpRequestException e)
                {
                    throw new InvalidOperationException("unable to retrieve the github pull requests", e);
                }
            }
        }

        private async Task<string> RunDeploymentCommand(string tag)
        {
            _logger.LogInformation("waiting for deployment lock");
            await _deploymentLock.WaitAsync();
            try
            {
                Dictionary<string, string> arguments = new Dictionary<string, string>()
                {
                    { "-k", _awsOptions.AWS_ACCESS_KEY_ID },
                    { "-s", _awsOptions.AWS_SECRET_ACCESS_KEY },
                    { "-r", _awsOptions.AWS_DEFAULT_REGION },
                    { "-c", _awsOptions.AWS_CLUSTER },
                    { "-n", _awsOptions.AWS_SERVICE },
                    { "-i", $"{_awsOptions.DOCKER_IMAGE}:{tag}" },
                    { "-D", $"{_awsOptions.DESIRED_COUNT}" },
                    { "-M", $"{(int)Math.Round(100.0 * (double)_awsOptions.MAX_COUNT / (double)_awsOptions.DESIRED_COUNT)}" },
                    { "-t", $"{_awsOptions.TIMEOUT}" }
                };

                string argumentString = $"/app/ecs-deploy {string.Join(" ", arguments.Select(arg => $"{arg.Key} {arg.Value}"))}";

                _logger.LogInformation("starting deployment: {0}", argumentString);

                Process process = new Process()
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

                using (process)
                {
                    process.Start();
                    Task<string> readStandardOut = process.StandardOutput.ReadToEndAsync();
                    Task<string> readStandardError = process.StandardError.ReadToEndAsync();
                    await Task.WhenAll(readStandardError, readStandardOut);

                    process.WaitForExit();

                    string standardOut = readStandardOut.Result;
                    string standardError = readStandardError.Result;

                    if (process.ExitCode != 0)
                        throw new InvalidOperationException($"deployment exited with error {process.ExitCode}: {standardError}, {standardOut}");

                    _logger.LogInformation("finished deployment: {0}, {1}", standardOut, standardError);
                    return standardOut;
                }
            }
            finally
            {
                _deploymentLock.Release();
            }
        }
    }
}