using System;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace Umbraco.Core.Cache
{
    /// <summary>
    /// An abstract class for implementing a runtime cache provider
    /// </summary>
    /// <remarks>
    /// </remarks>
    public interface IRuntimeCacheProvider : ICacheProvider
    {
        object GetCacheItem(
            string cacheKey, 
            Func<object> getCacheItem, 
            TimeSpan? timeout,
            bool isSliding = false,
            CacheItemPriority priority = CacheItemPriority.Normal,
            PostEvictionCallbackRegistration removedCallback = null,
            string[] dependentFiles = null);

        void InsertCacheItem(
            string cacheKey,
            Func<object> getCacheItem,
            TimeSpan? timeout = null,
            bool isSliding = false,
            CacheItemPriority priority = CacheItemPriority.Normal,
            PostEvictionCallbackRegistration removedCallback = null,
            string[] dependentFiles = null);

    }
}
