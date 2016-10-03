using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Umbraco.Core;
using Umbraco.Core.Logging;

namespace Umbraco.Web.Install
{
    /// <summary>
	/// Ensures authorization occurs for the installer if it has already completed. If install has not yet occured
	/// then the authorization is successful
	/// </summary>
	internal class InstallAuthorizeHandler : AuthorizationHandler<InstallAuthorizeHandler>, IAuthorizationRequirement
	{
        private readonly ApplicationContext _applicationContext;
        private readonly UmbracoContext _umbracoContext;

        public static InstallAuthorizeHandler CreateRequirement()
        {
            return new InstallAuthorizeHandler();   
        }

        private InstallAuthorizeHandler()
        {
        }
        
        public InstallAuthorizeHandler(UmbracoContext umbracoContext, ApplicationContext applicationContext)
        {
            if (umbracoContext == null) throw new ArgumentNullException("umbracoContext");
            if (applicationContext == null) throw new ArgumentNullException(nameof(applicationContext));

            _umbracoContext = umbracoContext;
            _applicationContext = applicationContext;            
        }     

        protected override void Handle(AuthorizationContext context, InstallAuthorizeHandler handler)
        {
            //if its not configured then we can continue
            if (_applicationContext.IsConfigured == false)
            {
                return;
            }

            return;
            //TODO: Implement this!

            ////otherwise we need to ensure that a user is logged in
            //var isLoggedIn = _umbracoContext.Security.ValidateCurrentUser();
            //if (isLoggedIn)
            //{
            //    return true;
            //}

            // Unauthorized!
            //context.Result = new UnauthorizedResult();
        }
	}
}