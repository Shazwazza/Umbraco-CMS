using LightInject;
using Umbraco.Core.Cache;

namespace Umbraco.Core.DependencyInjection
{
    public sealed class CacheCompositionRoot : ICompositionRoot
    {
        public const string StaticCache = "StaticCache";
        public const string RequestCache = "RequestCache";

        /// <summary>
        /// Composes services by adding services to the <paramref name="container" />.
        /// </summary>
        /// <param name="container">The target <see cref="T:LightInject.IServiceRegistry" />.</param>
        public void Compose(IServiceRegistry container)
        {
            container.RegisterSingleton<IRuntimeCacheProvider, ObjectCacheRuntimeCacheProvider>();
            container.RegisterSingleton<ICacheProvider, StaticCacheProvider>(StaticCache);
            container.RegisterSingleton<ICacheProvider, HttpRequestCacheProvider>(RequestCache);
            container.RegisterSingleton<IsolatedRuntimeCache>(factory =>
                new IsolatedRuntimeCache(type =>
                    //we need to have the dep clone runtime cache provider to ensure 
                    //all entities are cached properly (cloned in and cloned out)
                    new DeepCloneRuntimeCacheProvider(new ObjectCacheRuntimeCacheProvider())));
            
            container.RegisterSingleton<CacheHelper>(factory => new CacheHelper(
                factory.GetInstance<IRuntimeCacheProvider>(),
                factory.GetInstance<ICacheProvider>(StaticCache),
                factory.GetInstance<ICacheProvider>(RequestCache),
                factory.GetInstance<IsolatedRuntimeCache>()));
        }
    }
}