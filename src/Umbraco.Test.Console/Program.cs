using System;
using System.Collections;
using System.Data.SqlClient;
using System.Linq;
using LightInject;
using Umbraco.Core.DependencyInjection;
using Umbraco.Core.Configuration;
using LightInject.Microsoft.DependencyInjection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Core;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.PlatformAbstractions;
using Umbraco.Core.Cache;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Core.Persistence.UnitOfWork;
using Umbraco.Core.Logging;
using Umbraco.Core.Events;
using Umbraco.Core.Strings;
using System.Collections.Generic;
using Umbraco.Core.IO;
using Umbraco.Core.Configuration.UmbracoSettings;
using Umbraco.Core.Persistence.Mappers;
using Umbraco.Core.Persistence.Repositories;

namespace Umbraco.Test.Console
{
    // We are using this CLI parser: 
    //  https://github.com/aspnet/Common/blob/dev/src/Microsoft.Extensions.CommandLineUtils/CommandLine/CommandLineApplication.cs (https://www.nuget.org/packages/Microsoft.Extensions.CommandLineUtils/)
    // ... only docs I can find currently: 
    //  https://nocture.dk/2015/11/07/developing-and-distributing-tools-with-dnx-and-dnu/
    //  http://blog.devbot.net/console-services/
    //  https://github.com/GuardRex/GuardRex.AspNetCore.Server.IISIntegration.Tools/blob/master/Program.cs

    // This is the newer dotnetcore one: 
    //  https://github.com/dotnet/corefxlab/tree/master/src/System.CommandLine
    // but it's not on Nuget yet... looks nicer though            

    public class Program
    {
        public static void Main(string[] args)
        {
            //Create some in memory config
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection();
            var config = configBuilder.Build();
            
            System.Console.WriteLine("Loading...");

            var services = new ServiceCollection();
            //new umbraco app            
            var app = new UmbracoApplication(config);
            
            //setup Options            
            var umbracoSection = config.GetSection("umbraco");
            services.Configure<UmbracoConfigSection>(umbracoSection.GetSection("globalSettings"));            
            services.Configure<UmbracoSettingsSection>(umbracoSection.GetSection("umbracoSettings"));

            //setup DI/Container
            var serviceProvider = ConfigureServices(app, services);
            //boot the core
            serviceProvider.UseUmbracoCore(new ConsoleApplicationLifetime());

            //configure the command line app
            var cmd = ConfigureConsoleApp(serviceProvider);

            if (args.Length > 0)
            {
                cmd.RunArgs(args);
                System.Console.WriteLine("Press any key to exit");
                System.Console.ReadLine();
            }
            else
            {
                cmd.Prompt();
            }
        }

        private static IServiceProvider ConfigureServices(UmbracoApplication app, ServiceCollection services)
        {
            app.Container.RegisterSingleton(factory => PlatformServices.Default.Application);
            //register a faux IHostingEnvironment
            app.Container.RegisterSingleton<IHostingEnvironment, ConsoleHostingEnvironment>();
            
            var result = services.AddUmbracoCore(app);
            
            //replace with console objects

            //TODO: No matter what, i cannot just RegisterSingleton to override this registration even though it's not resolved yet, according to this it should
            // be possible: https://github.com/seesharper/LightInject/issues/133#issuecomment-63605627
            app.Container.Override(r => r.ServiceType == typeof(CacheHelper),
                (f, r) => new ServiceRegistration()
                {
                    ServiceType = typeof(CacheHelper),
                    ImplementingType = typeof(CacheHelper),
                    Lifetime = r.Lifetime,
                    ServiceName = r.ServiceName,
                    Value = new CacheHelper(
                        f.GetInstance<IRuntimeCacheProvider>(),
                        f.GetInstance<ICacheProvider>(CacheCompositionRoot.StaticCache),
                        f.GetInstance<ICacheProvider>(CacheCompositionRoot.StaticCache),
                        f.GetInstance<IsolatedRuntimeCache>())
                });            

            return result;
        }
        
        private static CommandLineApplication ConfigureConsoleApp(IServiceProvider services)
        {
            var app = new CommandLineApplication
            {
                Name = "umb",
                FullName = "Command line for Umbraco",
                Description = "Command line operations for Umbraco",
                ShortVersionGetter = () => "1.0.0"
            };

            app.HelpOption("-?|-h|--help");

            var appCtx = services.GetRequiredService<ApplicationContext>();
            services.GetRequiredService<IDatabaseUnitOfWorkProvider>();
            services.GetRequiredService<ILogger>();
            services.GetRequiredService<IEventMessagesFactory>();
            services.GetRequiredService<IDataTypeService>();
            services.GetRequiredService<IUserService>();
            services.GetRequiredService<IEnumerable<IUrlSegmentProvider>>();
            services.GetRequiredService<IOHelper>();
            services.GetRequiredService<MediaFileSystem>();
            services.GetRequiredService<IContentSection>();
            services.GetRequiredService<CacheHelper>();
            services.GetRequiredService<IMappingResolver>();            
            var contentService = services.GetRequiredService<IContentService>();

            app.UseDbCommand(appCtx);
            app.UseSchemaCommand(appCtx);
            app.UseBackCommand("quit", "Exits the application");
            
            //app.UseDbInstallCommand(_services.GetRequiredService<ApplicationContext>());
            //app.UseConnectCommand(_services.GetRequiredService<ApplicationContext>());

            return app;
        }
        
    }

    //borrowed from aspnet source
}
