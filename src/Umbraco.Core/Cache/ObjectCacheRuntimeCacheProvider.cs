using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Umbraco.Core.Logging;
using Umbraco.Core.Plugins;

namespace Umbraco.Core.Cache
{
    /// <summary>
    /// A cache provider that wraps the logic of a System.Runtime.Caching.ObjectCache
    /// </summary>
    internal class ObjectCacheRuntimeCacheProvider : IRuntimeCacheProvider
    {

        private readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        internal KeyedMemoryCache MemoryCache;

        /// <summary>
        /// Used for debugging
        /// </summary>
        internal Guid InstanceId { get; private set; }

        public ObjectCacheRuntimeCacheProvider()
        {
            MemoryCache = new KeyedMemoryCache(new MemoryCacheOptions());
            InstanceId = Guid.NewGuid();
        }

        #region Clear

        public virtual void ClearAllCache()
        {
            using (new WriteLock(_locker))
            {
                MemoryCache.DisposeIfDisposable();
                MemoryCache = new KeyedMemoryCache(new MemoryCacheOptions());
            }
        }

        public virtual void ClearCacheItem(string key)
        {
            using (new WriteLock(_locker))
            {
                object val;
                if (MemoryCache.TryGetValue(key, out val) == false)
                {
                    return;
                }                
                MemoryCache.Remove(key);
            }
        }

        public virtual void ClearCacheObjectTypes(string typeName)
        {
            //TODO: Does this work in aspnetcore?
            //var type = TypeFinder.GetTypeByName(typeName);
            var type = Type.GetType(typeName);

            if (type == null) return;
            var isInterface = type.GetTypeInfo().IsInterface;
            using (new WriteLock(_locker))
            {
                foreach (var key in MemoryCache.Keys
                    .Where(x =>
                    {
                        // x.Value is Lazy<object> and not null, its value may be null
                        // remove null values as well, does not hurt
                        // get non-created as NonCreatedValue & exceptions as null
                        object cacheVal;
                        if (MemoryCache.TryGetValue(x, out cacheVal))
                        {
                            var value = DictionaryCacheProviderBase.GetSafeLazyValue((Lazy<object>)cacheVal, true);

                            // if T is an interface remove anything that implements that interface
                            // otherwise remove exact types (not inherited types)
                            return value == null || (isInterface ? (type.IsInstanceOfType(value)) : (value.GetType() == type));
                        }
                        return false;
                    })
                    .ToArray()) // ToArray required to remove
                    MemoryCache.Remove(key);
            }
        }

        public virtual void ClearCacheObjectTypes<T>()
        {
            using (new WriteLock(_locker))
            {
                var typeOfT = typeof (T);
                var isInterface = typeOfT.GetTypeInfo().IsInterface;
                foreach (var key in MemoryCache.Keys
                    .Where(x =>
                    {
                        object cacheVal;
                        if (MemoryCache.TryGetValue(x, out cacheVal))
                        {
                            // x.Value is Lazy<object> and not null, its value may be null
                            // remove null values as well, does not hurt
                            // get non-created as NonCreatedValue & exceptions as null
                            var value = DictionaryCacheProviderBase.GetSafeLazyValue((Lazy<object>)cacheVal, true);

                            // if T is an interface remove anything that implements that interface
                            // otherwise remove exact types (not inherited types)
                            return value == null || (isInterface ? (value is T) : (value.GetType() == typeOfT));
                        }
                        return false;
                    })
                    .ToArray()) // ToArray required to remove
                    MemoryCache.Remove(key);
            }
        }

        public virtual void ClearCacheObjectTypes<T>(Func<string, T, bool> predicate)
        {
            using (new WriteLock(_locker))
            {
                var typeOfT = typeof(T);
                var isInterface = typeOfT.GetTypeInfo().IsInterface;
                foreach (var key in MemoryCache.Keys
                    .Where(x =>
                    {
                        object cacheVal;
                        if (MemoryCache.TryGetValue(x, out cacheVal))
                        {
                            // x.Value is Lazy<object> and not null, its value may be null
                            // remove null values as well, does not hurt
                            // get non-created as NonCreatedValue & exceptions as null
                            var value = DictionaryCacheProviderBase.GetSafeLazyValue((Lazy<object>)cacheVal, true);
                            if (value == null) return true;

                            // if T is an interface remove anything that implements that interface
                            // otherwise remove exact types (not inherited types)
                            return (isInterface ? (value is T) : (value.GetType() == typeOfT))
                                   && predicate(x, (T)value);
                        }
                        return false;
                    })
                    .ToArray()) // ToArray required to remove
                    MemoryCache.Remove(key);
            }
        }

        public virtual void ClearCacheByKeySearch(string keyStartsWith)
        {
            using (new WriteLock(_locker))
            {
                foreach (var key in MemoryCache.Keys
                    .Where(x => x.InvariantStartsWith(keyStartsWith))
                    .ToArray()) // ToArray required to remove
                    MemoryCache.Remove(key);
            }
        }

        public virtual void ClearCacheByKeyExpression(string regexString)
        {
            using (new WriteLock(_locker))
            {
                foreach (var key in MemoryCache.Keys
                    .Where(x => Regex.IsMatch(x, regexString))
                    .ToArray()) // ToArray required to remove
                    MemoryCache.Remove(key);
            }
        }

        #endregion

        #region Get

        public IEnumerable<object> GetCacheItemsByKeySearch(string keyStartsWith)
        {
            KeyValuePair<string, object>[] entries;
            using (new ReadLock(_locker))
            {
                entries = MemoryCache.Keys
                    .Where(x => x.InvariantStartsWith(keyStartsWith))
                    .Select(x =>
                    {
                        object cacheVal;
                        if (MemoryCache.TryGetValue(x, out cacheVal))
                        {
                            return new { key = x, val = cacheVal };
                        }
                        return null;
                    })
                    .WhereNotNull()
                    .Select(x => new KeyValuePair<string, object>(x.key, x.val))
                    .ToArray(); // evaluate while locked
            }
            return entries
                .Select(x => DictionaryCacheProviderBase.GetSafeLazyValue((Lazy<object>)x.Value)) // return exceptions as null
                .Where(x => x != null) // backward compat, don't store null values in the cache
                .ToList();
        }

        public IEnumerable<object> GetCacheItemsByKeyExpression(string regexString)
        {
            KeyValuePair<string, object>[] entries;
            using (new ReadLock(_locker))
            {
                entries = MemoryCache.Keys
                    .Where(x => Regex.IsMatch(x, regexString))
                    .Select(x =>
                    {
                        object cacheVal;
                        if (MemoryCache.TryGetValue(x, out cacheVal))
                        {
                            return new {key =  x, val = cacheVal};
                        }
                        return null;
                    })
                    .WhereNotNull()
                    .Select(x => new KeyValuePair<string, object>(x.key, x.val))
                    .ToArray(); // evaluate while locked
            }
            return entries
                .Select(x => DictionaryCacheProviderBase.GetSafeLazyValue((Lazy<object>)x.Value)) // return exceptions as null
                .Where(x => x != null) // backward compat, don't store null values in the cache
                .ToList();
        }

        public object GetCacheItem(string cacheKey)
        {
            Lazy<object> result;
            using (new ReadLock(_locker))
            {
                result = MemoryCache.Get(cacheKey) as Lazy<object>; // null if key not found
            }
            return result == null ? null : DictionaryCacheProviderBase.GetSafeLazyValue(result); // return exceptions as null
        }

        public object GetCacheItem(string cacheKey, Func<object> getCacheItem)
        {
            return GetCacheItem(cacheKey, getCacheItem, null);
        }

        public object GetCacheItem(string cacheKey, Func<object> getCacheItem, TimeSpan? timeout, bool isSliding = false, CacheItemPriority priority = CacheItemPriority.Normal,
            PostEvictionCallbackRegistration removedCallback = null, 
            string[] dependentFiles = null)
        {
            // see notes in HttpRuntimeCacheProvider

            Lazy<object> result;

            using (var lck = new UpgradeableReadLock(_locker))
            {
                result = MemoryCache.Get(cacheKey) as Lazy<object>;
                if (result == null || DictionaryCacheProviderBase.GetSafeLazyValue(result, true) == null) // get non-created as NonCreatedValue & exceptions as null
                {
                    result = DictionaryCacheProviderBase.GetSafeLazy(getCacheItem);
                    var policy = GetPolicy(timeout, isSliding, 
                        removedCallback, 
                        dependentFiles,
                        priority);

                    lck.UpgradeToWriteLock();
                    //NOTE: This does an add or update
                    MemoryCache.Set(cacheKey, result, policy);
                }
            }

            //return result.Value;

            var value = result.Value; // will not throw (safe lazy)
            var eh = value as DictionaryCacheProviderBase.ExceptionHolder;
            if (eh != null) throw eh.Exception; // throw once!
            return value;
        }

        #endregion

        #region Insert

        public void InsertCacheItem(string cacheKey, Func<object> getCacheItem, TimeSpan? timeout = null, bool isSliding = false, CacheItemPriority priority = CacheItemPriority.Normal,
            PostEvictionCallbackRegistration removedCallback = null, 
            string[] dependentFiles = null)
        {
            // NOTE - here also we must insert a Lazy<object> but we can evaluate it right now
            // and make sure we don't store a null value.

            var result = DictionaryCacheProviderBase.GetSafeLazy(getCacheItem);
            var value = result.Value; // force evaluation now
            if (value == null) return; // do not store null values (backward compat)

            var policy = GetPolicy(timeout, isSliding, 
                removedCallback, 
                dependentFiles, priority);
            //NOTE: This does an add or update
            MemoryCache.Set(cacheKey, result, policy);
        }

        #endregion

        private static MemoryCacheEntryOptions GetPolicy(TimeSpan? timeout = null, bool isSliding = false,
            PostEvictionCallbackRegistration removedCallback = null, 
            string[] dependentFiles = null,
            CacheItemPriority priority = CacheItemPriority.Normal)
        {
            
            var absolute = isSliding ? (DateTimeOffset?)null : (timeout == null ? (DateTimeOffset?)null : DateTime.Now.Add(timeout.Value));
            var sliding = isSliding == false ? (TimeSpan?)null : (timeout ?? (TimeSpan?)null);

            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = absolute,
                SlidingExpiration = sliding,
                Priority = priority
            };
            
            //TODO: Fix how dependent files can work with the new caching format!
            //if (dependentFiles != null && dependentFiles.Any())
            //{
            //    options.ExpirationTokens.Add(new ConfigurationReloadToken());
            //    policy.ChangeMonitors.Add(new HostFileChangeMonitor(dependentFiles.ToList()));
            //}
            
            if (removedCallback != null)
            {
                options.PostEvictionCallbacks.Add(removedCallback);                
            }
            return options;
        }
    }
}