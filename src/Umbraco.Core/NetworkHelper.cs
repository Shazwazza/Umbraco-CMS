using Microsoft.AspNet.Hosting;

namespace Umbraco.Core
{
    /// <summary>
    /// Currently just used to get the machine name in med trust and to format a machine name for use with file names
    /// </summary>
    public class NetworkHelper
    {
        private readonly IHostingEnvironment _hosting;

        public NetworkHelper(IHostingEnvironment hosting)
        {
            _hosting = hosting;
        }

        /// <summary>
        /// Returns the machine name that is safe to use in file paths.
        /// </summary>
        /// <remarks>
        /// see: https://github.com/Shandem/ClientDependency/issues/4
        /// </remarks>
        public string FileSafeMachineName
        {
            get { return _hosting.EnvironmentName.ReplaceNonAlphanumericChars('-'); }
        }
    }
}