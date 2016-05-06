using System;

namespace Umbraco.Core.Exceptions
{
    internal class DuplicateItemException : Exception
    {
        public DuplicateItemException(string message) : base(message)
        {
        }
    }
}