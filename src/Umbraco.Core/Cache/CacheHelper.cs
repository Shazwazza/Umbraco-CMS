using System;
using LightInject;
using Umbraco.Core.DependencyInjection;

namespace Umbraco.Core.Cache
{
	/// <summary>
	/// Class that is exposed by the ApplicationContext for application wide caching purposes
	/// </summary>
    public class CacheHelper
	{
        private static readonly ICacheProvider NullRequestCache = new NullCacheProvider();
        private static readonly ICacheProvider NullStaticCache = new NullCacheProvider();
        private static readonly IRuntimeCacheProvider NullRuntimeCache = new NullCacheProvider();
        
        /// <summary>
        /// Creates a cache helper with disabled caches
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Good for unit testing
        /// </remarks>
        public static CacheHelper CreateDisabledCacheHelper()
        {
            return new CacheHelper(NullRuntimeCache, NullStaticCache, NullRequestCache, new IsolatedRuntimeCache(t => NullRuntimeCache));
        }

	    /// <summary>
	    /// Initializes a new instance for use in the web
	    /// </summary>
	    public CacheHelper()
	        : this(
                new ObjectCacheRuntimeCacheProvider(),
	            new StaticCacheProvider(),
                new HttpRequestCacheProvider(),
                new IsolatedRuntimeCache(t => new ObjectCacheRuntimeCacheProvider()))
	    {
	    }
        
	    /// <summary>
	    /// Initializes a new instance based on the provided providers
	    /// </summary>
	    /// <param name="httpCacheProvider"></param>
	    /// <param name="staticCacheProvider"></param>
	    /// <param name="requestCacheProvider"></param>
	    /// <param name="isolatedCacheManager"></param>
	    public CacheHelper(
            IRuntimeCacheProvider httpCacheProvider,
            [Inject(CacheCompositionRoot.StaticCache)]ICacheProvider staticCacheProvider,
            [Inject(CacheCompositionRoot.RequestCache)]ICacheProvider requestCacheProvider,
            IsolatedRuntimeCache isolatedCacheManager)            
        {
	        if (httpCacheProvider == null) throw new ArgumentNullException("httpCacheProvider");
	        if (staticCacheProvider == null) throw new ArgumentNullException("staticCacheProvider");
	        if (requestCacheProvider == null) throw new ArgumentNullException("requestCacheProvider");
	        if (isolatedCacheManager == null) throw new ArgumentNullException("isolatedCacheManager");
	        RuntimeCache = httpCacheProvider;
            StaticCache = staticCacheProvider;
            RequestCache = requestCacheProvider;
            IsolatedRuntimeCache = isolatedCacheManager;
        }

        /// <summary>
        /// Returns the current Request cache
        /// </summary>
        public ICacheProvider RequestCache { get; internal set; }
        
        /// <summary>
        /// Returns the current Runtime cache
        /// </summary>
        public ICacheProvider StaticCache { get; internal set; }

        /// <summary>
        /// Returns the current Runtime cache
        /// </summary>
	    public IRuntimeCacheProvider RuntimeCache { get; internal set; }

        /// <summary>
        /// Returns the current Isolated Runtime cache manager
        /// </summary>
        public IsolatedRuntimeCache IsolatedRuntimeCache { get; internal set; }        
	    
    }

}
