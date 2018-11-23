using System;
using System.ComponentModel;
using System.Web;
using System.Web.Hosting;

namespace UmbracoExamine.DataServices
{
    public class UmbracoDataService : IDataService
    {
        public UmbracoDataService()
        {
            ContentService = new UmbracoContentService();
            MediaService = new UmbracoMediaService();
            LogService = new UmbracoLogService();
        }        

        public IContentService ContentService { get; protected set; }

        [Obsolete("This should no longer be used and will be removed in future versions")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public IMediaService MediaService { get; protected set; }

        public ILogService LogService { get; protected set; }

        [Obsolete("This should no longer be used and will be removed in future versions")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string MapPath(string virtualPath)
        {
            return HostingEnvironment.MapPath(virtualPath);
        }

    }
}
