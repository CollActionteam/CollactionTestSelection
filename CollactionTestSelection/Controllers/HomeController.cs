using CollactionTestSelection.Deployment;
using CollactionTestSelection.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CollactionTestSelection.Controllers
{
    public sealed class HomeController : Controller
    {
        private readonly IDeploymentService deploymentService;

        public HomeController(IDeploymentService deploymentService)
        {
            this.deploymentService = deploymentService;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View(new IndexViewModel(pullRequests: await deploymentService.GetPullRequests().ConfigureAwait(false)));
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Deploy(TagViewModel model)
        {
            if (!ModelState.IsValid)
                throw new InvalidOperationException("tag not specified or correct");

            return View(new DeployViewModel(tag: model.Tag, result: await deploymentService.RunDeploymentCommand(model.Tag).ConfigureAwait(false)));
        }
    }
}