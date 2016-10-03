using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Core;

namespace Umbraco.Test.Console
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var services = new ServiceCollection();
            var configBuilder = new ConfigurationBuilder();
            var config = configBuilder.Build();

            services.AddUmbracoCore(config);


        }
    }
}
