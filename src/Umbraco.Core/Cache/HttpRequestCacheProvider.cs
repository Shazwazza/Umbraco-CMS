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
    internal class HttpRequestCacheProvider : DictionaryCacheProviderBase
    {
        
        private readonly IHttpContextAccessor _context;
        private object _lock = new object();
        
        // for unit tests
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

        protected override IEnumerable<KeyValuePair<object, object>> GetDictionaryEntries()
        {
            const string prefix = CacheItemPrefix + "-";
            return _context.HttpContext.Items.Where(x => x.Key is string && ((string)x.Key).StartsWith(prefix));
        }

        protected override void RemoveEntry(string key)
        {
            _context.HttpContext.Items.Remove(key);
        }

        protected override object GetEntry(string key)
        {
            return _context.HttpContext.Items[key];
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
                return new MonitorLock(_lock);
            }
        }

        #endregion

        #region Get

        public override object GetCacheItem(string cacheKey, Func<object> getCacheItem)
        {
            cacheKey = GetCacheKey(cacheKey);

            Lazy<object> result;

            using (WriteLock)
            {
                result = _context.HttpContext.Items[cacheKey] as Lazy<object>; // null if key not found

                // cannot create value within the lock, so if result.IsValueCreated is false, just
                // do nothing here - means that if creation throws, a race condition could cause
                // more than one thread to reach the return statement below and throw - accepted.

                if (result == null || GetSafeLazyValue(result, true) == null) // get non-created as NonCreatedValue & exceptions as null
                {
                    result = GetSafeLazy(getCacheItem);
                    _context.HttpContext.Items[cacheKey] = result;
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

    }
}