using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Umbraco.Core.Collections;

namespace Umbraco.Core.Cache
{
    /// <summary>
    /// This is kind of silly but required for now because IMemoryCache does not expose keys, this might be available
    /// later: 
    /// https://github.com/aspnet/Caching/issues/155
    /// </summary>
    internal class KeyedMemoryCache : MemoryCache, IMemoryCache
    {
        public KeyedMemoryCache(IOptions<MemoryCacheOptions> optionsAccessor) : base(optionsAccessor)
        {
        }

        private readonly ConcurrentHashSet<string> _keys = new ConcurrentHashSet<string>();

        public IEnumerable<string> Keys => _keys;

        void IDisposable.Dispose()
        {
            _keys.Clear();
            base.Dispose();
        }

        ICacheEntry IMemoryCache.CreateEntry(object key)
        {
            _keys.TryAdd(key.ToString());
            return base.CreateEntry(key);
        }
        
        /// <summary>Gets the item associated with this key if present.</summary>
        /// <param name="key">An object identifying the requested entry.</param>
        /// <param name="value">The located value or null.</param>
        /// <returns>True if the key was found.</returns>
        bool IMemoryCache.TryGetValue(object key, out object value)
        {
            return base.TryGetValue(key, out value);
        }

        /// <summary>Removes the object associated with the given key.</summary>
        /// <param name="key">An object identifying the entry.</param>
        void IMemoryCache.Remove(object key)
        {
            _keys.Remove(key.ToString());
            base.Remove(key);
        }
    }
}