using CollactionTestSelection.Auth;
using CollactionTestSelection.Deployment;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CollactionTestSelection
{
    public sealed class Startup
    {
        private readonly IConfiguration configuration;

        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddAuthentication("Basic")
                    .AddScheme<BasicAuthenticationOptions, BasicAuthenticationHandler>(
                        "Basic",
                        o =>
                        {
                            o.Username = configuration["Username"];
                            o.Password = configuration["Password"];
                        });
            services.AddHealthChecks();
            services.AddLogging();
            services.AddTransient<IDeploymentService, DeploymentService>();
            services.Configure<DeployOptions>(configuration);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static")]
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
