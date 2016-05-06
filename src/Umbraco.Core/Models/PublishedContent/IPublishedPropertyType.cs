using System;

namespace Umbraco.Core.Models.PublishedContent
{
    public interface IPublishedPropertyType
    {
        /// <summary>
        /// Gets or sets the published content type containing the property type.
        /// </summary>
        // internally set by PublishedContentType constructor
        IPublishedContentType ContentType { get; }

        /// <summary>
        /// Gets or sets the alias uniquely identifying the property type.
        /// </summary>
        string PropertyTypeAlias { get; }

        /// <summary>
        /// Gets or sets the identifier uniquely identifying the data type supporting the property type.
        /// </summary>
        int DataTypeId { get; }

        /// <summary>
        /// Gets or sets the alias uniquely identifying the property editor for the property type.
        /// </summary>
        string PropertyEditorAlias { get; }

        Type ClrType { get; }

        object ConvertDataToSource(object source, bool preview);
        object ConvertSourceToObject(object source, bool preview);
        object ConvertSourceToXPath(object source, bool preview);
    }
}