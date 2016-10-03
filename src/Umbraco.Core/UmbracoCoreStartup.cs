using LightInject.Microsoft.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using LightInject;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Core.Cache;
using Umbraco.Core.DependencyInjection;
using Umbraco.Core.Logging;
using Umbraco.Core.Plugins;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbraco.Core.Configuration;
using Umbraco.Core.IO;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.AspNetCore.DataProtection;
using Umbraco.Core.Services;

namespace Umbraco.Core
{
    public static class UmbracoCoreStartup
    {
        /// <summary>
        /// Build the core container which contains all core things required to build an app context
        /// </summary>
        /// <remarks>
        /// In a web application, this is not called directly
        /// </remarks>
        public static IServiceProvider AddUmbracoCore(this IServiceCollection services, IConfiguration config)
        {
            var app = new UmbracoApplication(config);
            return services.AddUmbracoCore(app);
        }

        /// <summary>
        /// This sets up the default ASP.Net container to use LightInject
        /// </summary>
        /// <param name="services"></param>
        /// <param name="container"></param>
        /// <remarks>
        /// Developers may choose to replace that during startup but by default Umbraco will make ASP.Net use the LightInject container
        /// </remarks>
        public static IServiceProvider WrapAspNetContainer(this IServiceCollection services, IServiceContainer container)
        {
            return container.CreateServiceProvider(services);
        }

        /// <summary>
        /// Build the core container which contains all core things requird to build an app context
        /// </summary>
        public static IServiceProvider AddUmbracoCore(this IServiceCollection services, UmbracoApplication umbracoApplication)
        {
            var umbContainer = umbracoApplication.Container;

            ////ensure it's not run twice
            //if (umbContainer.AvailableServices.Any())
            //    return services.WrapAspNetContainer(umbracoApplication.Container);

            // For now we're gonna put our app in the aspnet default container
            services.AddTransient<UmbracoApplication>(factory => umbracoApplication);
            umbContainer.Register<UmbracoApplication>(factory => umbracoApplication);

            //register our own container in our own container too
            umbContainer.Register<IServiceContainer>(factory => umbContainer);
            
            //umbContainer.Register<ApplicationEnvironment>(factory => factory.GetInstance<UmbracoApplication>().ApplicationEnvironment);
            //register aspnet bits - TODO: DO we really want this registered for Umbraco Core?
            //umbContainer.RegisterSingleton<IHttpContextAccessor, HttpContextAccessor>();

            //Umbraco Logging
            umbContainer.RegisterSingleton<ILogger>(factory => umbracoApplication.Logger);
            umbContainer.RegisterSingleton<IProfiler>(factory => umbracoApplication.Profiler);
            umbContainer.RegisterSingleton<ProfilingLogger>(factory => new ProfilingLogger(factory.GetInstance<ILogger>(), factory.GetInstance<IProfiler>()));

            //Config
            umbContainer.RegisterFrom(new ConfigurationCompositionRoot(umbracoApplication.Configuration));

            //Cache
            umbContainer.RegisterFrom<CacheCompositionRoot>();

            //Datalayer/Repositories/SQL/Database/etc...
            umbContainer.RegisterFrom<RepositoryCompositionRoot>();

            //Data Services/ServiceContext/etc...
            umbContainer.RegisterFrom<ServicesCompositionRoot>();

            //ModelMappers
            //container.RegisterFrom<CoreModelMappersCompositionRoot>();

            umbContainer.RegisterSingleton<ITypeFinder>(factory => new TypeFinder(
                factory.GetInstance<ILogger>(),
                factory.GetInstance<IEnumerable<IAssemblyProvider>>()));
            umbContainer.RegisterSingleton<IAssemblyProvider, DefaultUmbracoAssemblyProvider>();
            umbContainer.RegisterSingleton<PluginManager>();
            umbContainer.RegisterSingleton<IOHelper>();
            umbContainer.RegisterSingleton<EnvironmentHelper>(factory => new EnvironmentHelper(
                factory.GetInstance<IHostingEnvironment>(),
                //Getting the application Id in aspnetcore is certainly not normal, here's the code that does this:
                // https://github.com/aspnet/DataProtection/blob/82d92064c50c13f2737f96c6d76b45d68e9a9d05/src/Microsoft.AspNetCore.DataProtection.Interfaces/DataProtectionExtensions.cs#L97
                // here's the comment that says it shouldn't be in hosting: https://github.com/aspnet/Hosting/issues/177#issuecomment-80738319
                //Seems as though we can also use IApplicationDiscriminator which is what this does in this method but it also contains fallbacks
                new Umbraco.Core.DependencyInjection.LightInjectServiceProvider(factory).GetApplicationUniqueIdentifier()));
            umbContainer.RegisterSingleton<ApplicationContext>();

            //TODO: We need to use Options<T> for IFileSystem implementations!

            return services.WrapAspNetContainer(umbracoApplication.Container);
        }

        /// <summary>
        /// Start the umbraco application and ensure it's only started once
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        /// <param name="applicationLifetime"></param>
        public static void UseUmbracoCore(this IApplicationBuilder app, /*ApplicationEnvironment env,*/ IApplicationLifetime applicationLifetime)
        {
            if (app.Properties.ContainsKey("umbraco-core-started"))
                throw new InvalidOperationException($"{nameof(UseUmbracoCore)} has already been called");

            app.ApplicationServices.UseUmbracoCore(/*env,*/ applicationLifetime);                       

            app.Properties["umbraco-core-started"] = true;
        }

        /// <summary>
        /// Start the umbraco application
        /// </summary>
        /// <param name="services"></param>
        /// <param name="env"></param>
        /// <param name="applicationLifetime"></param>
        public static void UseUmbracoCore(this IServiceProvider services, /*ApplicationEnvironment env,*/ IApplicationLifetime applicationLifetime)
        {            
            var umbApp = services.GetRequiredService<UmbracoApplication>();

            //Boot!
            umbApp.StartApplication(/*env,*/ applicationLifetime);            
        }
    }
}