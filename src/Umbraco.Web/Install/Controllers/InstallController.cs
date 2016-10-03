using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Core.IO;

namespace Umbraco.Web.Install.Controllers
{
    /// <summary>
    /// The MVC Installation controller
    /// </summary>
    /// <remarks>
    /// NOTE: All views must have their full paths as we do not have a custom view engine for the installation views!
    /// </remarks>
    [Authorize("Umbraco-Installation")]
    public class InstallController : Controller
    {
        private readonly UmbracoContext _umbracoContext;
        private readonly IOHelper _ioHelper;

        public InstallController(UmbracoContext umbracoContext, IOHelper ioHelper)
        {
            _umbracoContext = umbracoContext;
            _ioHelper = ioHelper;        
        }


        [HttpGet]
        public ActionResult Index()
        {
            if (ApplicationContext.Current.IsConfigured)
            {
                return Redirect(SystemDirectories.Umbraco.EnsureEndsWith('/'));   
            }

            //TODO: Make this happen
            //if (ApplicationContext.Current.IsUpgrading)
            //{
            //    var result = _umbracoContext.Security.ValidateCurrentUser(false);

            //    switch (result)
            //    {
            //        case ValidateRequestAttempt.FailedNoPrivileges:
            //        case ValidateRequestAttempt.FailedNoContextId:
            //            //TODO: Fix this up with correct config
            //            return Redirect("umbraco/" + "/AuthorizeUpgrade?redir=" + UriHelper.Encode(Request.PathBase, Request.Path, Request.QueryString));
            //    }
            //}
       

            //gen the install base url
            ViewBag.InstallApiBaseUrl = Url.GetUmbracoApiService("GetSetup", "InstallApi", "UmbracoInstall").TrimEnd("GetSetup");
            
            //get the base umbraco folder
            ViewBag.UmbracoBaseFolder = _ioHelper.ResolveUrl(SystemDirectories.Umbraco);

            InstallHelper ih = new InstallHelper(_umbracoContext);
            ih.InstallStatus(false, "");

            //always ensure full path (see NOTE in the class remarks)
            //TODO: Fix this up with correct config
            //return View(GlobalSettings.Path.EnsureEndsWith('/') + "install/views/index.cshtml");            
            return View("umbraco/" + "install/views/index.cshtml");
        }

    }
}
