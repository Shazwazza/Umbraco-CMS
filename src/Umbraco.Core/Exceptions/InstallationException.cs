using System;

namespace Umbraco.Core.Exceptions
{
    internal class InstallationException : Exception
    {
        public InstallationException(string message): base(message)
        {
            
        }
    }
}