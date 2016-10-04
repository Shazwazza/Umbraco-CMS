using LightInject;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Core.Configuration;
using Umbraco.Core.Configuration.UmbracoSettings;

namespace Umbraco.Core.DependencyInjection
{
    /// <summary>
    /// Sets up IoC container for Umbraco configuration classes
    /// </summary>
    public sealed class ConfigurationCompositionRoot : ICompositionRoot
    {
        private readonly IConfiguration _config;
        private readonly IServiceCollection _services;

        public ConfigurationCompositionRoot(IConfiguration config, IServiceCollection services)
        {
            _config = config;
            _services = services;            
        }

        public void Compose(IServiceRegistry container)
        {
            container.RegisterSingleton<IUmbracoConfig>(factory => factory.GetInstance<IOptions<UmbracoConfigSection>>().Value);
            container.RegisterSingleton<IConnectionString>(factory => factory.GetInstance<IUmbracoConfig>().ConnectionString);

            container.RegisterSingleton<IUmbracoSettingsSection>(factory => factory.GetInstance<IOptions<UmbracoSettingsSection>>().Value);
            container.RegisterSingleton<IContentSection>(factory => factory.GetInstance<IUmbracoSettingsSection>().Content);

            //container.Register<ITemplatesSection>(factory => factory.GetInstance<IUmbracoSettingsSection>().Templates);
            //container.Register<IRequestHandlerSection>(factory => factory.GetInstance<IUmbracoSettingsSection>().RequestHandler);

            //TODO: Add the other config areas...
        }
    }
}