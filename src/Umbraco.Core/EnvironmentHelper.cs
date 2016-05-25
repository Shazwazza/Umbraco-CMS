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

        public EnvironmentHelper(IHostingEnvironment hosting, IServiceProvider serviceProvider)
        {
            _hosting = hosting;
            //Getting the application Id in aspnetcore is certainly not normal, here's the code that does this:
            // https://github.com/aspnet/DataProtection/blob/82d92064c50c13f2737f96c6d76b45d68e9a9d05/src/Microsoft.AspNetCore.DataProtection.Interfaces/DataProtectionExtensions.cs#L97
            // here's the comment that says it shouldn't be in hosting: https://github.com/aspnet/Hosting/issues/177#issuecomment-80738319
            //Seems as though we can also use IApplicationDiscriminator which is what this does in this method but it also contains fallbacks
            ApplicationId = serviceProvider.GetApplicationUniqueIdentifier();
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