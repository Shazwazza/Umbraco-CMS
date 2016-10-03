using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using System.Buffers;

namespace Umbraco.Web.WebApi
{
    /// <summary>
    /// Applying this attribute to any webapi controller will ensure that it only contains one json formatter compatible with the angular json vulnerability prevention.
    /// </summary>
    public class AngularJsonOnlyConfigurationAttribute : Attribute, IActionFilter
    {
        
        public void OnActionExecuting(ActionExecutingContext context)
        {
        }

        /// <summary>
        /// Called after the action executes, before the action result.
        /// </summary>
        /// <param name="context">The <see cref="T:Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext" />.</param>
        public void OnActionExecuted(ActionExecutedContext context)
        {
            var result = context.Result as ObjectResult;
            if (result != null)
            {
                //remove all json/xml formatters then add our custom one
                var toRemove = result.Formatters.Where(t => (t is JsonOutputFormatter)).ToList();
                foreach (var r in toRemove)
                {
                    result.Formatters.Remove(r);
                }

                result.Formatters.Add(new AngularJsonMediaTypeFormatter(
                    //TODO: Fix these params, they should be injected
                    new Newtonsoft.Json.JsonSerializerSettings(), ArrayPool<char>.Shared));
            }
           
        }
    }
}