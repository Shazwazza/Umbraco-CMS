using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Umbraco.Core.Plugins
{
    /// <summary>
    /// Provides a list of loaded assemblies that can be scanned
    /// </summary>
    public interface IAssemblyProvider
    {
        IEnumerable<Assembly> Assemblies { get; }
    }
}
