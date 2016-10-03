using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.PlatformAbstractions;

namespace Umbraco.Test.Console
{
    public class ConsoleHostingEnvironment : IHostingEnvironment
    {
        public ConsoleHostingEnvironment(ApplicationEnvironment appEnvironment)
        {
            EnvironmentName = "Console";
            ApplicationName = appEnvironment.ApplicationName;
            WebRootPath = Path.Combine(appEnvironment.ApplicationBasePath, "wwwroot");
            Directory.CreateDirectory(WebRootPath);
            WebRootFileProvider = new PhysicalFileProvider(WebRootPath);
            ContentRootPath = appEnvironment.ApplicationBasePath;
            ContentRootFileProvider = new PhysicalFileProvider(ContentRootPath);
        }

        public string EnvironmentName { get; set; }

        public string ApplicationName { get; set; }

        public string WebRootPath { get; set; }

        public IFileProvider WebRootFileProvider { get; set; }

        public string ContentRootPath { get; set; }

        public IFileProvider ContentRootFileProvider { get; set; }
    }
}