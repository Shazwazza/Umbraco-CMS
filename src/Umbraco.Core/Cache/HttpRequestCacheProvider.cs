using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Umbraco.Core.Cache
{
    /// <summary>
    /// A cache provider that caches items in the HttpContext.Items
    /// </summary>
    /// <remarks>
    /// If the Items collection is null, then this provider has no effect
    /// </remarks>
    internal class HttpRequestCacheProvider : DictionaryCacheProviderBase
    {
        private readonly IHttpContextAccessor _context;
        private readonly object _lock = new object();

        private IDictionary<object, object> ContextItems => _context.HttpContext.Items;

        private bool HasContextItems => _context != null;

        public HttpRequestCacheProvider(IHttpContextAccessor context)
        {
            _context = context;
        }

        // main constructor
        // will use HttpContext.Current
        public HttpRequestCacheProvider(/*Func<HttpContext> context*/)
        {
            //_context2 = context;
        }

        protected override IEnumerable<DictionaryEntry> GetDictionaryEntries()
        {
            const string prefix = CacheItemPrefix + "-";

            if (HasContextItems == false) return Enumerable.Empty<DictionaryEntry>();

            return ContextItems.Cast<DictionaryEntry>()
                .Where(x => x.Key is string && ((string)x.Key).StartsWith(prefix));
        }

        protected override void RemoveEntry(string key)
        {
            if (HasContextItems == false) return;

            ContextItems.Remove(key);
        }

        protected override object GetEntry(string key)
        {
            return HasContextItems ? ContextItems[key] : null;
        }

        #region Lock

        protected override IDisposable ReadLock
        {
            // there's no difference between ReadLock and WriteLock here
            get { return WriteLock; }
        }

        protected override IDisposable WriteLock
        {
            // NOTE
            //   could think about just overriding base.Locker to return a different
            //   object but then we'd create a ReaderWriterLockSlim per request,
            //   which is less efficient than just using a basic monitor lock.

            get
            {
                return HasContextItems
                    ? (IDisposable) new MonitorLock(_lock)
                    : new NoopLocker();
            }
        }

        #endregion

        #region Get

        public override object GetCacheItem(string cacheKey, Func<object> getCacheItem)
        {
            //no place to cache so just return the callback result
            if (HasContextItems == false) return getCacheItem();

            cacheKey = GetCacheKey(cacheKey);

            Lazy<object> result;

            using (WriteLock)
            {
                result = ContextItems[cacheKey] as Lazy<object>; // null if key not found

                // cannot create value within the lock, so if result.IsValueCreated is false, just
                // do nothing here - means that if creation throws, a race condition could cause
                // more than one thread to reach the return statement below and throw - accepted.

                if (result == null || GetSafeLazyValue(result, true) == null) // get non-created as NonCreatedValue & exceptions as null
                {
                    result = GetSafeLazy(getCacheItem);
                    ContextItems[cacheKey] = result;
                }
            }

            // using GetSafeLazy and GetSafeLazyValue ensures that we don't cache
            // exceptions (but try again and again) and silently eat them - however at
            // some point we have to report them - so need to re-throw here

            // this does not throw anymore
            //return result.Value;

            var value = result.Value; // will not throw (safe lazy)
            var eh = value as ExceptionHolder;
            if (eh != null) throw eh.Exception; // throw once!
            return value;
        }

        #endregion

        #region Insert
        #endregion

        private class NoopLocker : DisposableObject
        {
            protected override void DisposeResources()
            { }
        }
    }
}
