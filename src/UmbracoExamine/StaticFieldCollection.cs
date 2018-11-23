using System.Collections.ObjectModel;
using Examine.LuceneEngine;

namespace UmbracoExamine
{
    public class StaticFieldCollection : KeyedCollection<string, StaticField>
    {
        public static StaticFieldCollection CreateDefaultUmbracoContentIndexFieldPolicies()
        {
            return new StaticFieldCollection
            {
                new StaticField("id", FieldIndexTypes.NOT_ANALYZED, false, string.Empty),
                new StaticField("key", FieldIndexTypes.NOT_ANALYZED, false, string.Empty),
                new StaticField("version", FieldIndexTypes.NOT_ANALYZED, false, string.Empty),
                new StaticField("parentID", FieldIndexTypes.NOT_ANALYZED, false, string.Empty),
                new StaticField("level", FieldIndexTypes.NOT_ANALYZED, true, "NUMBER"),
                new StaticField("writerID", FieldIndexTypes.NOT_ANALYZED, false, string.Empty),
                new StaticField("creatorID", FieldIndexTypes.NOT_ANALYZED, false, string.Empty),
                new StaticField("nodeType", FieldIndexTypes.NOT_ANALYZED, false, string.Empty),
                new StaticField("template", FieldIndexTypes.NOT_ANALYZED, false, string.Empty),
                new StaticField("sortOrder", FieldIndexTypes.NOT_ANALYZED, true, "NUMBER"),
                new StaticField("createDate", FieldIndexTypes.NOT_ANALYZED, false, "DATETIME"),
                new StaticField("updateDate", FieldIndexTypes.NOT_ANALYZED, false, "DATETIME"),
                new StaticField("nodeName", FieldIndexTypes.ANALYZED, false, string.Empty),
                new StaticField("urlName", FieldIndexTypes.NOT_ANALYZED, false, string.Empty),
                new StaticField("writerName", FieldIndexTypes.ANALYZED, false, string.Empty),
                new StaticField("creatorName", FieldIndexTypes.ANALYZED, false, string.Empty),
                new StaticField("nodeTypeAlias", FieldIndexTypes.ANALYZED, false, string.Empty),
                new StaticField("path", FieldIndexTypes.NOT_ANALYZED, false, string.Empty),
                new StaticField("isPublished", FieldIndexTypes.NOT_ANALYZED, false, string.Empty)
            };
        }

        protected override string GetKeyForItem(StaticField item)
        {
            return item.Name;
        }

        /// <summary>
        /// Implements TryGetValue using the underlying dictionary
        /// </summary>
        /// <param name="key"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public bool TryGetValue(string key, out StaticField field)
        {
            if (Dictionary == null)
            {
                field = null;
                return false;
            }
            return Dictionary.TryGetValue(key, out field);
        }
    }
}
