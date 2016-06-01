using System;
using LightInject;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Core.Cache;
using Umbraco.Core.DependencyInjection;
using Umbraco.Core.Logging;
using Umbraco.Core.Plugins;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Umbraco.Core
{
    public static class UmbracoCoreStartup
    {
        public static void AddUmbracoCore(this IServiceCollection services)
        {
            var app = new UmbracoApplication();
            services.AddUmbracoCore(app);
        }

        /// <summary>
        /// Build the core container which contains all core things requird to build an app context
        /// </summary>
        public static void AddUmbracoCore(this IServiceCollection services, UmbracoApplication umbracoApplication)
        {
            //TODO: hrm, should we do anything with the aspnetcore service container? Maybe not here
            // but we could allow devs to do this: http://www.lightinject.net/microsoft.dependencyinjection/
            // maybe in the above method since this method 'could' be used for extensibility if devs created
            // their own instance of UmbracoApplication? we'll see. 

            var container = umbracoApplication.Container;

            container.Register<IServiceContainer>(factory => container);

            //Umbraco Logging
            container.RegisterSingleton<ILogger>(factory => umbracoApplication.Logger);
            container.RegisterSingleton<IProfiler>(factory => umbracoApplication.Profiler);
            container.RegisterSingleton<ProfilingLogger>(factory => new ProfilingLogger(factory.GetInstance<ILogger>(), factory.GetInstance<IProfiler>()));

            //Config
            container.RegisterFrom(new ConfigurationCompositionRoot(umbracoApplication.Configuration));

            //Cache
            container.RegisterFrom<CacheCompositionRoot>();
            
            //Datalayer/Repositories/SQL/Database/etc...
            container.RegisterFrom<RepositoryCompositionRoot>();

            //Data Services/ServiceContext/etc...
            container.RegisterFrom<ServicesCompositionRoot>();

            //ModelMappers
            //container.RegisterFrom<CoreModelMappersCompositionRoot>();

            container.RegisterSingleton<IServiceProvider, ActivatorServiceProvider>();
            container.RegisterSingleton<PluginManager>();

            container.RegisterSingleton<ApplicationContext>();

            //TODO: We need to use Options<T> for IFileSystem implementations!

        }

        public static void UseUmbracoCore(this IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime applicationLifetime)
        {
            
        }
    }
}