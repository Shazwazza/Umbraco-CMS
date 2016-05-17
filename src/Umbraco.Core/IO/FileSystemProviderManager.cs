using System;
using System.Collections.Generic;
using System.Linq;

namespace Umbraco.Core.IO
{	
    public class FileSystemProviderManager
    {
        private readonly IEnumerable<FileSystemWrapper> _fileSystems;

        internal FileSystemProviderManager(IEnumerable<FileSystemWrapper> fileSystems)
        {
            _fileSystems = fileSystems;
        }

        /// <summary>
        /// Returns the strongly typed file system provider
        /// </summary>
        /// <typeparam name="TProviderTypeFilter"></typeparam>
        /// <returns></returns>
        public TProviderTypeFilter GetFileSystemProvider<TProviderTypeFilter>()
			where TProviderTypeFilter : FileSystemWrapper
        {
            var fs = _fileSystems.FirstOrDefault(x => x.GetType() == typeof(TProviderTypeFilter));
            if (fs == null) throw new ArgumentException("No file system found of type " + typeof(TProviderTypeFilter));
            return (TProviderTypeFilter)fs;
        }
    }
}
