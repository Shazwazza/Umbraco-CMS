using System.Collections.Generic;

namespace Umbraco.Core.Models.PublishedContent
{
    public interface IPublishedContentType
    {
        int Id { get; }
        string Alias { get; }
        HashSet<string> CompositionAliases { get; }
        IEnumerable<IPublishedPropertyType> PropertyTypes { get; }
        int GetPropertyIndex(string alias);
        IPublishedPropertyType GetPropertyType(string alias);
        IPublishedPropertyType GetPropertyType(int index);
    }
}