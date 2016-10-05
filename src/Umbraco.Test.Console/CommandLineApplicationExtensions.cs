using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.CommandLineUtils;
using Umbraco.Core;

namespace Umbraco.Test.Console
{
    /// <summary>
    /// Extension methods for working with the command line application
    /// </summary>
    public static class CommandLineApplicationExtensions
    {
        /// <summary>
        /// Used to add the callback for a command and ensure that argument and option values are reset after
        /// executing so they can be re-used.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="callback"></param>
        public static void OnExecuteAndReset(this CommandLineApplication c, Func<int> callback)
        {
            c.OnExecute(() =>
            {
                var result = callback();

                foreach (var commandArgument in c.Arguments)
                {
                    commandArgument.Values.Clear();
                }
                foreach (var commandOption in c.Options)
                {
                    commandOption.Values.Clear();
                }

                return result;
            });            
        }

        public static void Prompt(this CommandLineApplication c, bool showHelp = false)
        {
            if (c.Name.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("The " + typeof(CommandLineApplication).Name + " must have a Name property assigned");
            }

            if (showHelp)
            {
                c.ShowHelp();
                System.Console.WriteLine();
            }

            while (true)
            {                
                var prompts = new List<string>() {c.Name};
                var parent = c.Parent;
                while (parent != null)
                {
                    prompts.Add(parent.Name);
                    parent = parent.Parent;
                }
                prompts.Reverse();
                System.Console.Write($"{string.Join("/", prompts)}> ");
                
                var val = System.Console.ReadLine();

                var args = SplitArguments(val);
                if (args.Length <= 0)
                    continue;

                var result = c.RunArgs(args);
                if (result >= 100)
                    return;

                System.Console.WriteLine();
            }
        }

        /// <summary>
        /// Execute the args for the app
        /// </summary>
        /// <param name="args"></param>
        /// <param name="c"></param>
        public static int RunArgs(this CommandLineApplication c, string[] args)
        {
            try
            {
                var result = c.Execute(args);
                return result;
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError(ex);
                return 0;
            }
        }

        //borrowed from: http://stackoverflow.com/a/2132004/694494
        private static string[] SplitArguments(string commandLine)
        {
            var parmChars = commandLine.ToCharArray();
            var inSingleQuote = false;
            var inDoubleQuote = false;
            for (var index = 0; index < parmChars.Length; index++)
            {
                if (parmChars[index] == '"' && !inSingleQuote)
                {
                    inDoubleQuote = !inDoubleQuote;
                    parmChars[index] = '\n';
                }
                if (parmChars[index] == '\'' && !inDoubleQuote)
                {
                    inSingleQuote = !inSingleQuote;
                    parmChars[index] = '\n';
                }
                if (!inSingleQuote && !inDoubleQuote && parmChars[index] == ' ')
                    parmChars[index] = '\n';
            }
            return (new string(parmChars)).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        
    }
}