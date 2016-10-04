using System.Collections.Generic;

namespace Umbraco.Test.Console
{
    public static class ConsoleHelper
    {       

        public static void WriteDictionaryVals(IDictionary<string, object> nameVals)
        {
            foreach (var item in nameVals)
            {
                System.Console.WriteLine($"{item.Key}: {item.Value}");
            }
        }

        public static void WriteError(object err)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("---------------------->");
            System.Console.WriteLine("ERROR: " + err);
            System.Console.WriteLine("<----------------------");
            System.Console.WriteLine();
        }
    }
}