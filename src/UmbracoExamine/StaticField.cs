using Examine;
using Examine.LuceneEngine;

namespace UmbracoExamine
{
    public class StaticField : IIndexField
    {
        public StaticField(string name, FieldIndexTypes indexType, bool enableSorting, string type)
        {
            Type = type;
            EnableSorting = enableSorting;
            IndexType = indexType;
            Name = name;
        }

        public string Name { get; set; }
        public FieldIndexTypes IndexType { get; }
        public bool EnableSorting { get; set; }
        public string Type { get; set; }
    }
}
