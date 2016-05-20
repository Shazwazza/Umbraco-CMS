using System;
using System.Collections.Generic;
using System.Linq;
using LightInject;
using Umbraco.Core.Logging;
using Umbraco.Core.ObjectResolution;

namespace Umbraco.Core.PropertyEditors
{
    /// <summary>
    /// A resolver to resolve all registered validators
    /// </summary>
    internal class ValidatorsResolver : ContainerLazyManyObjectsResolver<ValidatorsResolver, ManifestValueValidator>
    {
        public ValidatorsResolver(IServiceContainer container, ILogger logger, Func<IEnumerable<Type>> typeListProducerList)
            : base(container, logger, typeListProducerList, ObjectLifetimeScope.Application)
        {
        }

        /// <summary>
        /// Returns the validators
        /// </summary>
        public IEnumerable<ManifestValueValidator> Validators
        {
            get { return Values; }
        }

        /// <summary>
        /// Gets a validator by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ManifestValueValidator GetValidator(string name)
        {
            return Values.FirstOrDefault(x => x.TypeName.InvariantEquals(name));
        } 
    }
}