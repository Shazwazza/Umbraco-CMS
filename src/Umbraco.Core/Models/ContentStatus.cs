using System;
using System.Runtime.Serialization;

namespace Umbraco.Core.Models
{
    /// <summary>
    /// Enum for the various statuses a Content object can have
    /// </summary>
    #if NET461
    [Serializable] 
#endif
    [DataContract]
    public enum ContentStatus
    {
        [EnumMember]
        Unpublished,
        [EnumMember]
        Published,
        [EnumMember]
        Expired,
        [EnumMember]
        Trashed,
        [EnumMember]
        AwaitingRelease
    }
}