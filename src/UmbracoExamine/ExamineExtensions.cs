using System.Collections.Specialized;
using Examine;
using Umbraco.Core;

namespace UmbracoExamine
{
    public static class ExamineExtensions
    {
        /// <summary>
        /// Returns true if the Umbraco application is in a state that we can initialize the examine indexes
        /// </summary>
        /// <returns></returns>
        public static bool CanInitialize(this ApplicationContext appCtx)
        {
            //We need to check if we actually can initialize, if not then don't continue
            return appCtx != null
                   && appCtx.IsConfigured
                   && appCtx.DatabaseContext.IsDatabaseConfigured;
        }

        public static void ConfigureOptions(this NameValueCollection config, out bool supportUnpublishedContent, out bool supportProtectedContent)
        {
            //check if there's a flag specifying to support unpublished content,
            //if not, set to false;
            bool supportUnpublished;
            if (config["supportUnpublished"] != null && bool.TryParse(config["supportUnpublished"], out supportUnpublished))
                supportUnpublishedContent = supportUnpublished;
            else
                supportUnpublishedContent = false;


            //check if there's a flag specifying to support protected content,
            //if not, set to false;
            bool supportProtected;
            if (config["supportProtected"] != null && bool.TryParse(config["supportProtected"], out supportProtected))
                supportProtectedContent = supportProtected;
            else
                supportProtectedContent = false;
        }
    }
}
