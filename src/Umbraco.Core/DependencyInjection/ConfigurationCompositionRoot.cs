using LightInject;
using Microsoft.Extensions.Configuration;
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

        public ConfigurationCompositionRoot(IConfiguration config)
        {
            _config = config;
        }

        public void Compose(IServiceRegistry container)
        {
            container.RegisterSingleton<IUmbracoConfig>(factory => new UmbracoConfigSection(_config));
            container.RegisterSingleton<IConnectionString>(factory => factory.GetInstance<IUmbracoConfig>().ConnectionString);

            //container.Register<IUmbracoSettingsSection>(factory => UmbracoConfig.For.UmbracoSettings());
            //container.Register<IContentSection>(factory => factory.GetInstance<IUmbracoSettingsSection>().Content);
            //container.Register<ITemplatesSection>(factory => factory.GetInstance<IUmbracoSettingsSection>().Templates);
            //container.Register<IRequestHandlerSection>(factory => factory.GetInstance<IUmbracoSettingsSection>().RequestHandler);

            //TODO: Add the other config areas...
        }
    }
}