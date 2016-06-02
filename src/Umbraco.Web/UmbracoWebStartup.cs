using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Core;

namespace Umbraco.Web
{
    public static class UmbracoWebStartup
    {
        public static void AddUmbracoWeb(this IServiceCollection services, IConfiguration config)
        {
            var app = new UmbracoApplication(config);
            services.AddUmbracoCore(app);

            var umbContainer = app.Container;
            
        }

        /// <summary>
        /// Build the web container which contains all core things requird to build an app context
        /// </summary>
        public static void AddUmbracoWeb(this IServiceCollection services, UmbracoApplication umbracoApplication)
        {
            var umbContainer = umbracoApplication.Container;
            
            //no need to declare as per request, currently we manage it's lifetime as the singleton
            //umbContainer.Register<UmbracoContext>(new PerRequestLifeTime());

        }
    }
}