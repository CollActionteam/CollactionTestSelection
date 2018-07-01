using CollactionTestSelection.Options;
using EdjCase.BasicAuth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace CollactionTestSelection
{
    public sealed class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddAuthentication(BasicAuthConstants.AuthScheme)
                    .AddBasicAuth(options =>
                    {
                        options.AuthenticateCredential = Authenticate;
                    });
            services.Configure<GithubOptions>(Configuration);
            services.Configure<DeployOptions>(Configuration);
            services.Configure<JiraOptions>(Configuration);
            services.AddSingleton(new SemaphoreSlim(1, 1)); // Deployment lock
            services.AddLogging();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseDeveloperExceptionPage()
               .UseAuthentication()
               .UseMvc(routes =>
               {
                   routes.MapRoute(
                       name: "default",
                       template: "{controller=Deployment}/{action=Index}");
               });
        }

        private Task<AuthenticationTicket> Authenticate(BasicAuthInfo authInfo)
        {
            AuthenticationTicket ticket;
            string username = Configuration["USERNAME"];
            string password = Configuration["PASSWORD"];
            if (authInfo.Credential.Username == username && authInfo.Credential.Password == password)
            {
                ClaimsIdentity identity = new ClaimsIdentity(authInfo.Scheme);
                identity.AddClaim(new Claim("Name", username));
                ClaimsPrincipal principal = new ClaimsPrincipal(identity);
                ticket = new AuthenticationTicket(principal, authInfo.Properties, authInfo.Scheme);
            }
            else
                ticket = null;

            return Task.FromResult(ticket);
        }
    }
}
