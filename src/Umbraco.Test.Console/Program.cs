using System;
using System.Collections;
using System.Data.SqlClient;
using System.Linq;
using LightInject;
using Umbraco.Core.DependencyInjection;
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


namespace Umbraco.Test.Console
{
    // We are using this CLI parser: 
    //  https://github.com/aspnet/Common/blob/dev/src/Microsoft.Extensions.CommandLineUtils/CommandLine/CommandLineApplication.cs (https://www.nuget.org/packages/Microsoft.Extensions.CommandLineUtils/)
    // ... only docs I can find currently: 
    //  https://nocture.dk/2015/11/07/developing-and-distributing-tools-with-dnx-and-dnu/
    //  http://blog.devbot.net/console-services/

    // This is the newer dotnetcore one: 
    //  https://github.com/dotnet/corefxlab/tree/master/src/System.CommandLine
    // but it's not on Nuget yet... looks nicer though            

    public class Program
    {
        private static IServiceProvider _services;        

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
            //setup DI/Container
            _services = ConfigureServices(app, services);
            //boot the core
            _services.UseUmbracoCore(new ConsoleApplicationLifetime());

            //configure the command line app
            var cmd = ConfigureConsoleApp();

            if (args.Length > 0)
            {
                RunArgs(args, cmd);
                System.Console.WriteLine("Press any key to exit");
                System.Console.ReadLine();
            }
            else
            {
                Prompt(cmd);
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

        private static void Prompt(CommandLineApplication c)
        {
            //Show help as default message
            c.ShowHelp();

            while (true)
            {                
                var val = System.Console.ReadLine();

                var args = SplitArguments(val);
                if (args.Length <= 0)
                    continue;

                var result = RunArgs(args, c);
                if (result >= 100)
                    return;

                System.Console.WriteLine("Execution complete");
                System.Console.WriteLine();
            }
        }

        private static CommandLineApplication ConfigureConsoleApp()
        {
            var app = new CommandLineApplication
            {
                Name = "umbraco-cli",
                FullName = "Command line for Umbraco",
                Description = "Command line operations for Umbraco",
                ShortVersionGetter = () => "1.0.0"
            };

            app.HelpOption("-?|-h|--help");

            app.UseQuitCommand();
            app.UseDbInstallCommand(_services.GetRequiredService<ApplicationContext>());
            app.UseConnectCommand(_services.GetRequiredService<ApplicationContext>());

            return app;
        }
        
        /// <summary>
        /// Execute the args for the app
        /// </summary>
        /// <param name="args"></param>
        /// <param name="c"></param>
        private static int RunArgs(string[] args, CommandLineApplication c)
        {
            try
            {
                var result = c.Execute(args);
                return result;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError(ex);
                return 0;
            }
        }

        //borrowed from: http://stackoverflow.com/a/2132004/694494
        private static string[] SplitArguments(string commandLine)
        {
            var parmChars = commandLine.ToCharArray();
            var inSingleQuote = false;
            var inDoubleQuote = false;
            for (var index = 0; index < parmChars.Length; index++)
            {
                if (parmChars[index] == '"' && !inSingleQuote)
                {
                    inDoubleQuote = !inDoubleQuote;
                    parmChars[index] = '\n';
                }
                if (parmChars[index] == '\'' && !inDoubleQuote)
                {
                    inSingleQuote = !inSingleQuote;
                    parmChars[index] = '\n';
                }
                if (!inSingleQuote && !inDoubleQuote && parmChars[index] == ' ')
                    parmChars[index] = '\n';
            }
            return (new string(parmChars)).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }

    //borrowed from aspnet source
}
