
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Http;

namespace Umbraco.Core
{
    public class DefaultScopeContextAdapter : IScopeContextAdapter
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AsyncLocal<IDictionary<string,object>> _nonWebContext = new AsyncLocal<IDictionary<string, object>>();

        /// <summary>
        /// Lazily creates a non-web (singleton) context value container
        /// </summary>
        protected IDictionary<string, object> NonWebContext => 
            _nonWebContext.Value ?? (_nonWebContext.Value = new Dictionary<string, object>());

        /// <summary>
        /// Constructor for web applications
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        public DefaultScopeContextAdapter(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Constructor for non-web applications
        /// </summary>
        public DefaultScopeContextAdapter()
        {
            _httpContextAccessor = null;
        }

        public object Get(string key)
        {
            return _httpContextAccessor?.HttpContext == null
                ? NonWebContext.GetValue(key)
                : _httpContextAccessor.HttpContext.Items[key];
        }

        public void Set(string key, object value)
        {
            if (_httpContextAccessor?.HttpContext == null)
            {
                if (value != null)
                    NonWebContext[key] = value;
                else
                    NonWebContext.Remove(key);
            }
            else
            {
                if (value != null)
                    _httpContextAccessor.HttpContext.Items[key] = value;
                else
                    _httpContextAccessor.HttpContext.Items.Remove(key);
            }
        }

        public void Clear(string key)
        {
            if (_httpContextAccessor?.HttpContext == null)
                NonWebContext.Remove(key);
            else
                _httpContextAccessor.HttpContext.Items.Remove(key);
        }
    }
}
