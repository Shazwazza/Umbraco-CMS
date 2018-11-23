using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.Repositories;
using UmbracoExamine;
using UmbracoExamine.DataServices;

namespace Umbraco.Tests.UmbracoExamine
{
    /// <summary>
	/// A mock data service used to return content from the XML data file created with CWS
	/// </summary>
	public class TestContentService : IContentService
	{
		public const int ProtectedNode = 1142;

		public TestContentService(string contentXml = null, string mediaXml = null)
		{
            if (contentXml == null)
            {
                contentXml = TestFiles.umbraco;
            }
            if (mediaXml == null)
            {
                mediaXml = TestFiles.media;
            }
            _xContent = XDocument.Parse(contentXml);
		    _xMedia = XDocument.Parse(mediaXml);
		}

        #region IContentService Members

	    [Obsolete("This method is not be used, it will be removed in future versions")]
	    [EditorBrowsable(EditorBrowsableState.Never)]
        public XDocument GetLatestContentByXPath(string xpath)
		{
			var xdoc = XDocument.Parse("<content></content>");
			xdoc.Root.Add(_xContent.XPathSelectElements(xpath));

			return xdoc;
		}

        [Obsolete("This method is not be used, it will be removed in future versions")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public XDocument GetPublishedContentByXPath(string xpath)
		{
		    return GetContentByXPath(xpath, _xContent);			
		}

        private XDocument GetContentByXPath(string xpath, XDocument content)
        {
            var xdoc = XDocument.Parse("<content></content>");
            xdoc.Root.Add(content.XPathSelectElements(xpath));

            return xdoc;
        }

		public string StripHtml(string value)
		{
			const string pattern = @"<(.|\n)*?>";
			return Regex.Replace(value, pattern, string.Empty);
		}

		public bool IsProtected(int nodeId, string path)
		{
			// single node is marked as protected for test indexer
			// hierarchy is not important for this test
			return nodeId == ProtectedNode;
		}

        private List<string> _userPropNames; 
		public IEnumerable<string> GetAllUserPropertyNames()
		{
            if (_userPropNames == null)
            {
                var xpath = "//*[count(@id)>0 and @id != -1]";
                _userPropNames = GetPublishedContentByXPath(xpath)
                    .Root
                    .Elements() //each page
                    .SelectMany(x => x.Elements().Where(e => e.Attribute("id") == null)) //each page property (no @id)         
                    .Select(x => x.Name.LocalName) //the name of the property
                    .Distinct()
                    .Union(GetContentByXPath(xpath, _xMedia)
                               .Root
                               .Elements() //each page
                               .SelectMany(x => x.Elements().Where(e => e.Attribute("id") == null)) //each page property (no @id)         
                               .Select(x => (string)x.Attribute("alias")) //the name of the property NOTE: We are using the legacy XML here.
                               .Distinct()).ToList();
            }
		    return _userPropNames;
		}

        private List<string> _sysPropNames; 
		public IEnumerable<string> GetAllSystemPropertyNames()
		{
            if (_sysPropNames == null)
            {
                _sysPropNames = UmbracoContentIndexer.IndexFieldPolicies.Select(x => x.Name).ToList();
            }
		    return _sysPropNames;
		}

		#endregion

		private readonly XDocument _xContent;
        private readonly XDocument _xMedia;






	}
}
