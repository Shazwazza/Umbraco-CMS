using System;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System.ComponentModel;

namespace UmbracoExamine.DataServices
{
    public interface IContentService 
    {
        [Obsolete("This should no longer be used, latest content will be indexed by using Umbraco's own IContentService")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        XDocument GetLatestContentByXPath(string xpath);

        [Obsolete("This method is not be used, it will be removed in future versions")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        XDocument GetPublishedContentByXPath(string xpath);

        /// <summary>
        /// Returns a list of ALL properties names for all nodes defined in the data source
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetAllUserPropertyNames();

        /// <summary>
        /// Returns a list of ALL system property names for all nodes defined in the data source
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetAllSystemPropertyNames();

        string StripHtml(string value);
        bool IsProtected(int nodeId, string path);
    }
}
