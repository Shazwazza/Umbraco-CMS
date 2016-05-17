using System;
using Umbraco.Core.Models.PublishedContent;

namespace Umbraco.Core.PropertyEditors
{
    /// <summary>
    /// Provides a default overridable implementation for <see cref="IPropertyValueConverter"/> that does nothing.
    /// </summary>
    public class PropertyValueConverterBase : IPropertyValueConverter
    {
        public virtual bool IsConverter(IPublishedPropertyType propertyType)
        {
            return false;
        }

        public virtual object ConvertDataToSource(IPublishedPropertyType propertyType, object source, bool preview)
        {
            throw new NotImplementedException("FIX PropertyValueConverterBase.ConvertDataToSource");
            //return PublishedPropertyType.ConvertUsingDarkMagic(source);
        }

        public virtual object ConvertSourceToObject(IPublishedPropertyType propertyType, object source, bool preview)
        {
            return source;
        }

        public virtual object ConvertSourceToXPath(IPublishedPropertyType propertyType, object source, bool preview)
        {
            return source.ToString();
        }
    }
}
