using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Umbraco.Core
{
    public static class UmbracoCoreStartup
    {
        public static UmbracoApplication AddUmbraco(
            this IServiceCollection services, 
            IHostingEnvironment hostingEnvironment,
            IConfiguration umbracoConfig, 
            IApplicationLifetime applicationLifetime)
        {
            return new UmbracoApplication(hostingEnvironment, applicationLifetime, umbracoConfig);
        }
    }
}