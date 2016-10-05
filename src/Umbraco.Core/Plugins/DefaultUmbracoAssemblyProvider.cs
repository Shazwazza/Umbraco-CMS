using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.PlatformAbstractions;

namespace Umbraco.Core.Plugins
{
    /// <summary>
    /// Discovers assemblies that are part of the Umbraco application using the DependencyContext.
    /// </summary>
    /// <remarks>
    /// Happily "borrowed" from aspnet: https://github.com/aspnet/Mvc/blob/230a13d0e13e4c7e192bc6623762bfd4cde726ef/src/Microsoft.AspNetCore.Mvc.Core/Internal/DefaultAssemblyPartDiscoveryProvider.cs
    /// 
    /// TODO: Happily borrow their unit tests too
    /// </remarks>
    public class DefaultUmbracoAssemblyProvider : IAssemblyProvider
    {
        private readonly ApplicationEnvironment _appEnv;

        public DefaultUmbracoAssemblyProvider(ApplicationEnvironment hosting)
        {
            _appEnv = hosting;
        }

        public IEnumerable<Assembly> Assemblies => DiscoverAssemblyParts(_appEnv.ApplicationName);

        internal static HashSet<string> ReferenceAssemblies { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Umbraco.Core"          
        };

        internal static IEnumerable<Assembly> DiscoverAssemblyParts(string entryPointAssemblyName)
        {
            var entryAssembly = Assembly.Load(new AssemblyName(entryPointAssemblyName));
            var context = DependencyContext.Load(entryAssembly);
            
            var candidates = GetCandidateAssemblies(entryAssembly, context);

            return candidates;
        }

        internal static IEnumerable<Assembly> GetCandidateAssemblies(Assembly entryAssembly, DependencyContext dependencyContext)
        {
            if (dependencyContext == null)
            {
                // Use the entry assembly as the sole candidate.
                return new[] { entryAssembly };
            }

            //includeRefLibs == true - so that Umbraco.Core is also returned!
            return GetCandidateLibraries(dependencyContext, includeRefLibs: true)
                .SelectMany(library => library.GetDefaultAssemblyNames(dependencyContext))
                .Select(Assembly.Load);
        }

        /// <summary>
        /// Returns a list of libraries that references the assemblies in <see cref="ReferenceAssemblies"/>.       
        /// </summary>
        /// <param name="dependencyContext"></param>
        /// <param name="includeRefLibs">
        /// True to also include libs in the ReferenceAssemblies list
        /// </param>
        /// <returns></returns>
        internal static IEnumerable<RuntimeLibrary> GetCandidateLibraries(DependencyContext dependencyContext, bool includeRefLibs)
        {
            if (ReferenceAssemblies == null)
            {
                return Enumerable.Empty<RuntimeLibrary>();
            }

            var candidatesResolver = new CandidateResolver(dependencyContext.RuntimeLibraries, ReferenceAssemblies, includeRefLibs);
            return candidatesResolver.GetCandidates();
        }

        private class CandidateResolver
        {
            private readonly bool _includeRefLibs;
            private readonly IDictionary<string, Dependency> _dependencies;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="dependencies"></param>
            /// <param name="referenceAssemblies"></param>
            /// <param name="includeRefLibs">
            /// True to also include libs in the ReferenceAssemblies list
            /// </param>
            public CandidateResolver(IEnumerable<RuntimeLibrary> dependencies, ISet<string> referenceAssemblies, bool includeRefLibs)
            {
                _includeRefLibs = includeRefLibs;                

                _dependencies = dependencies
                    .ToDictionary(d => d.Name, d => CreateDependency(d, referenceAssemblies), StringComparer.OrdinalIgnoreCase);
            }

            /// <summary>
            /// Create a Dependency
            /// </summary>
            /// <param name="library"></param>
            /// <param name="referenceAssemblies"></param>            
            /// <returns></returns>
            private static Dependency CreateDependency(RuntimeLibrary library, ISet<string> referenceAssemblies)
            {
                var classification = DependencyClassification.Unknown;
                if (referenceAssemblies.Contains(library.Name))
                {
                    classification = DependencyClassification.UmbracoReference;
                }

                return new Dependency(library, classification);
            }

            private DependencyClassification ComputeClassification(string dependency)
            {
                Debug.Assert(_dependencies.ContainsKey(dependency));

                var candidateEntry = _dependencies[dependency];
                if (candidateEntry.Classification != DependencyClassification.Unknown)
                {
                    return candidateEntry.Classification;
                }
                else
                {
                    var classification = DependencyClassification.NotCandidate;
                    foreach (var candidateDependency in candidateEntry.Library.Dependencies)
                    {
                        var dependencyClassification = ComputeClassification(candidateDependency.Name);
                        if (dependencyClassification == DependencyClassification.Candidate ||
                            dependencyClassification == DependencyClassification.UmbracoReference)
                        {
                            classification = DependencyClassification.Candidate;
                            break;
                        }
                    }

                    candidateEntry.Classification = classification;

                    return classification;
                }
            }

            public IEnumerable<RuntimeLibrary> GetCandidates()
            {
                foreach (var dependency in _dependencies)
                {
                    var classification = ComputeClassification(dependency.Key);
                    if (classification == DependencyClassification.Candidate ||
                        //if the flag is set, also ensure to include any UmbracoReference classifications
                            (_includeRefLibs && classification == DependencyClassification.UmbracoReference))
                    {
                        yield return dependency.Value.Library;
                    }
                }
            }

            private class Dependency
            {
                public Dependency(RuntimeLibrary library, DependencyClassification classification)
                {
                    Library = library;
                    Classification = classification;
                }

                public RuntimeLibrary Library { get; }

                public DependencyClassification Classification { get; set; }

                public override string ToString()
                {
                    return $"Library: {Library.Name}, Classification: {Classification}";
                }
            }

            private enum DependencyClassification
            {
                Unknown = 0,
                Candidate = 1,
                NotCandidate = 2,
                UmbracoReference = 3
            }
        }
    }
}