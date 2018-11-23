using System;
using System.ComponentModel;
using System.Xml.Linq;
namespace UmbracoExamine.DataServices
{
    [Obsolete("This should no longer be used, latest content will be indexed by Umbraco's own IMediaService")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IMediaService 
    {
        [Obsolete("This should no longer be used, latest content will be indexed by Umbraco's own IMediaService")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        XDocument GetLatestMediaByXpath(string xpath);
    }
}
