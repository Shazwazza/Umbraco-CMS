using System;
using System.Runtime.Serialization;

namespace Umbraco.Core.Models
{
  
    /// <summary>
    /// Represents a Partial View file
    /// </summary>
    #if NET461
    [Serializable] 
#endif
    [DataContract(IsReference = true)]
    public class PartialView : File, IPartialView
    {
        public PartialView(string path)
            : this(path, null)
        { }

        internal PartialView(string path, Func<File, string> getFileContent)
            : base(path, getFileContent)
        { }

        internal PartialViewType ViewType { get; set; }
    }
}