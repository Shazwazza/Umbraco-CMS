using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Examine;
using Examine.LuceneEngine.Config;
using Examine.Providers;
using Umbraco.Core;
using UmbracoExamine.DataServices;

namespace UmbracoExamine
{
    public static class ExamineHelper
    {
        /// <summary>
        /// A helper method to be used for GatheringNodeData for Umbraco content
        /// </summary>
        /// <param name="indexer"></param>
        /// <param name="contentService"></param>
        /// <param name="e"></param>
        public static void AddIndexDataForContent(this BaseIndexProvider indexer, IContentService contentService, IndexingNodeDataEventArgs e)
        {
            //strip html of all users fields if we detect it has HTML in it. 
            //if that is the case, we'll create a duplicate 'raw' copy of it so that we can return
            //the value of the field 'as-is'.
            // Get all user data that we want to index and store into a dictionary 
            foreach (var field in indexer.IndexerData.UserFields)
            {
                if (e.Fields.TryGetValue(field.Name, out var fieldVal))
                {
                    //check if the field value has html
                    if (XmlHelper.CouldItBeXml(fieldVal))
                    {
                        //First save the raw value to a raw field, we will change the policy of this field by detecting the prefix later
                        e.Fields[UmbracoContentIndexer.RawFieldPrefix + field.Name] = fieldVal;
                        //now replace the original value with the stripped html
                        e.Fields[field.Name] = contentService.StripHtml(fieldVal);
                    }
                }
            }

            //ensure the special path and node type alias fields is added to the dictionary to be saved to file
            var path = e.Node.Attribute("path").Value;
            if (e.Fields.ContainsKey(UmbracoContentIndexer.IndexPathFieldName) == false)
                e.Fields.Add(UmbracoContentIndexer.IndexPathFieldName, path);

            //this needs to support both schema's so get the nodeTypeAlias if it exists, otherwise the name
            var nodeTypeAlias = e.Node.Attribute("nodeTypeAlias") == null ? e.Node.Name.LocalName : e.Node.Attribute("nodeTypeAlias").Value;
            if (e.Fields.ContainsKey(UmbracoContentIndexer.NodeTypeAliasFieldName) == false)
                e.Fields.Add(UmbracoContentIndexer.NodeTypeAliasFieldName, nodeTypeAlias);

            //add icon 
            var icon = (string)e.Node.Attribute("icon");
            if (e.Fields.ContainsKey(UmbracoContentIndexer.IconFieldName) == false)
                e.Fields.Add(UmbracoContentIndexer.IconFieldName, icon);

            //add guid 
            var guid = (string)e.Node.Attribute("key");
            if (e.Fields.ContainsKey(UmbracoContentIndexer.NodeKeyFieldName) == false)
                e.Fields.Add(UmbracoContentIndexer.NodeKeyFieldName, guid);

            if (e.Fields.ContainsKey("nodeName"))
            {
                //add the __nodeName as lower case
                e.Fields[UmbracoContentIndexer.NodeNameFieldName] = e.Fields["nodeName"].ToLower();
            }
        }

        private string GetValue()
    }
}
