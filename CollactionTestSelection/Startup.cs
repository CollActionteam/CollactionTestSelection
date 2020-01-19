using CollactionTestSelection.Auth;
using CollactionTestSelection.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            services.AddAuthentication("Basic")
                    .AddScheme<BasicAuthenticationOptions, BasicAuthenticationHandler>(
                        "Basic",
                        o =>
                        {
                            o.Username = Configuration["USERNAME"];
                            o.Password = Configuration["PASSWORD"];
                        });
            services.AddHealthChecks();
            services.Configure<GithubOptions>(Configuration);
            services.Configure<DeployOptions>(Configuration);
            services.Configure<JiraOptions>(Configuration);
            services.Configure<NetiflyOptions>(Configuration);
            services.AddSingleton(new SemaphoreSlim(1, 1)); // Deployment lock
            services.AddLogging();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting()
               .UseDeveloperExceptionPage()
               .UseAuthentication()
               .UseAuthorization()
               .UseEndpoints(routes =>
               {
                   routes.MapDefaultControllerRoute();
                   routes.MapHealthChecks("/health");
               });
        }
    }
}
