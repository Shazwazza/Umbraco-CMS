using System;

namespace Umbraco.Core.Exceptions
{
    internal class FileSecurityException : Exception
    {
        public FileSecurityException(string message)
            : base(message)
        {
        }
    }
}