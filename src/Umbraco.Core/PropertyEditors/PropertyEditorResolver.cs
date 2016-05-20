using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LightInject;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.IO;
using Umbraco.Core.Manifest;
using Umbraco.Core.ObjectResolution;

namespace Umbraco.Core.PropertyEditors
{
    /// <summary>
    /// A resolver to resolve all property editors
    /// </summary>
    /// <remarks>
    /// This resolver will contain any property editors defined in manifests as well!
    /// </remarks>
    public class PropertyEditorResolver : ContainerLazyManyObjectsResolver<PropertyEditorResolver, PropertyEditor>
    {        
        internal PropertyEditorResolver(IServiceContainer container, ILogger logger, Func<IEnumerable<Type>> typeListProducerList)
            : base(container, logger, typeListProducerList, ObjectLifetimeScope.Application)
        {
            _unioned = new Lazy<List<PropertyEditor>>(() => Values.Union(container.GetInstance<ManifestBuilder>().PropertyEditors).ToList());
        }

        private readonly Lazy<List<PropertyEditor>> _unioned;

        /// <summary>
        /// Returns the property editors
        /// </summary>
        public virtual IEnumerable<PropertyEditor> PropertyEditors
        {
            get { return _unioned.Value; }
        }

        /// <summary>
        /// Returns a property editor by alias
        /// </summary>
        /// <param name="alias"></param>
        /// <returns></returns>
        public virtual PropertyEditor GetByAlias(string alias)
        {
            return PropertyEditors.SingleOrDefault(x => x.Alias == alias);
        }
    }
}