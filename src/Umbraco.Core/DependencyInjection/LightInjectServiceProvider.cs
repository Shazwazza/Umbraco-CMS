using System;
using LightInject;

namespace Umbraco.Core.DependencyInjection
{
    /// <summary>
    /// This is used as a work around for: https://github.com/seesharper/LightInject/issues/283
    /// </summary>
    public sealed class LightInjectServiceProvider : IServiceProvider
    {
        private readonly IServiceFactory _factory;

        public LightInjectServiceProvider(IServiceFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            _factory = factory;
        }

        public object GetService(Type serviceType)
        {
            return _factory.TryGetInstance(serviceType);
        }
    }
}