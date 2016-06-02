using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;

namespace Umbraco.Core
{
    /// <summary>
    /// Used to resolve some information about the environment
    /// </summary>
    public class EnvironmentHelper
    {
        private readonly IHostingEnvironment _hosting;

        public EnvironmentHelper(IHostingEnvironment hosting, string applicationId)
        {
            _hosting = hosting;            
            ApplicationId = applicationId.ToMd5();
        }

        /// <summary>
        /// Gets the unique application ID for this website on this machine
        /// </summary>
        public string ApplicationId { get; }

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