using System;

namespace Umbraco.Core.Events
{
	#if NET461
    [Serializable] 
#endif
	public delegate void TypedEventHandler<in TSender, in TEventArgs>(TSender sender, TEventArgs e);
}