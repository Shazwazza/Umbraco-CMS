using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Umbraco.Core.IO
{
    //all paths has a starting but no trailing /
	public class SystemDirectories
    {

        public static string Config => "~/config";

	    public static string Css => "~/css";

	    public static string Data => "~/App_Data";

	    public static string Install => "~/install";

		public static string AppPlugins => "~/App_Plugins";

	    public static string MvcViews => "~/Views";
        
	    public static string Media => "~/media";

	    public static string Scripts => "~/scripts";

	    public static string Umbraco => "~/umbraco";
       
    }


    
}
