using System;
using LightInject;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Core;
//using Umbraco.Web.Install;

namespace Umbraco.Web
{
    public static class UmbracoWebStartup
    {
        /// <summary>
        /// Build the web container which contains all core things required to build an app context
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config"></param>
        public static IServiceProvider AddUmbracoWeb(this IServiceCollection services, IConfiguration config)
        {
            var app = new UmbracoApplication(config);

            services.AddUmbracoCore(app);
            services.AddUmbracoWeb(app);

            return services.WrapAspNetContainer(app.Container);
        }

        /// <summary>
        /// Build the web container which contains all core things required to build an app context
        /// </summary>
        internal static void AddUmbracoWeb(this IServiceCollection services, UmbracoApplication umbracoApplication)
        {
            services.AddMvc();

            var umbContainer = umbracoApplication.Container;
            
            //no need to declare as per request, currently we manage it's lifetime as the singleton
            umbContainer.Register<UmbracoContext>(new PerRequestLifeTime());

            //umbContainer.Register<InstallAuthorizeHandler>(new PerRequestLifeTime());

            services.ConfigureAuthorization();
        }

        private static void ConfigureAuthorization(this IServiceCollection services)
        {
            //services.AddAuthorization(options =>
            //{
            //    options.AddPolicy("Umbraco-Installation",
            //        policy => policy.Requirements.Add(InstallAuthorizeHandler.CreateRequirement()));
            //});

            //services.AddTransient<IAuthorizationHandler, InstallAuthorizeHandler>();
        }

        /// <summary>
        /// Start the umbraco application
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        /// <param name="applicationLifetime"></param>
        public static void UseUmbracoWeb(this IApplicationBuilder app, /*IHostingEnvironment env,*/ IApplicationLifetime applicationLifetime)
        {
            if (app.Properties.ContainsKey("umbraco-web-started"))
                throw new InvalidOperationException($"{nameof(UseUmbracoWeb)} has already been called");

            app.UseUmbracoCore(/*env,*/ applicationLifetime);

            app.Properties["umbraco-web -started"] = true;            
        }
    }
}