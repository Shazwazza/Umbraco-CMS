using System;
using System.ComponentModel;
using System.Web;


namespace UmbracoExamine.DataServices
{
    public interface IDataService
    {        
        IContentService ContentService { get; }
        ILogService LogService { get; }

        [Obsolete("This should no longer be used, latest content will be indexed by Umbraco's own IMediaService")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        IMediaService MediaService { get; }

        [Obsolete("This should no longer be used and will be removed in future versions")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        string MapPath(string virtualPath);
    }
}
