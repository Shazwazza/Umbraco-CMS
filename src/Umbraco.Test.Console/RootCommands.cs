using System;
using System.Collections.Generic;
using Microsoft.Extensions.CommandLineUtils;
using Umbraco.Core;

namespace Umbraco.Test.Console
{
    /// <summary>
    /// Extension methods used to create commands
    /// </summary>
    public static class RootCommands
    {   

        public static void UseSchemaCommand(this CommandLineApplication app, ApplicationContext appContext)
        {
            app.Command("schema", c =>
            {
                c.Description = "Commands for working with the Umbraco Schema objects";
                c.HelpOption("-h");

                c.UseDocumentTypeCommand(appContext);
                c.UseMediaTypeCommand(appContext);
                c.UseBackCommand();

                c.OnExecute(() =>
                {
                    c.Prompt();

                    return 1;
                });
            });
        }

        public static void UseDbCommand(this CommandLineApplication app, ApplicationContext appContext)
        {
            app.Command("db", c =>
            {
                c.Description = "Commands for working with the Umbraco Db";
                c.HelpOption("-h");
                c.UseDbInstallCommand(appContext);
                c.UseConnectCommand(appContext);
                c.UseBackCommand();

                c.OnExecute(() =>
                {
                    c.Prompt();

                    return 1;
                });
            });
        }

        public static void UseConnectCommand(this CommandLineApplication app, ApplicationContext appContext)
        {
            app.Command("connect", c =>
            {
                c.Description = "Connects to an existing Umbraco Db";
                var argConnString = c.Argument("[connectionstring]", "The database connection string");
                c.HelpOption("-h");

                c.OnExecuteAndReset(() =>
                {
                    ConnectDb(argConnString.Value, appContext);

                    System.Console.WriteLine("Validating schema...");
                    var schemaResult = appContext.DatabaseContext.ValidateDatabaseSchema();

                    if (schemaResult.ValidTables.Count <= 0 && schemaResult.ValidColumns.Count <= 0 && schemaResult.ValidConstraints.Count <= 0 && schemaResult.ValidIndexes.Count <= 0)
                    {
                        System.Console.WriteLine("The database is not installed, run db-install");
                        return 0;
                    }

                    return 1;
                });
            });
        }

        /// <summary>
        /// Moves the cursor back on level (or exits)
        /// </summary>
        /// <param name="app"></param>
        /// <param name="name"></param>
        /// <param name="desc"></param>
        public static void UseBackCommand(this CommandLineApplication app, string name = "/", string desc = "Moves the cursor back one level")
        {
            app.Command(name, c =>
            {
                c.Description = desc;
                c.OnExecute(() => 101);
            });
        }

        public static void UseDbInstallCommand(this CommandLineApplication app, ApplicationContext appContext)
        {
            app.Command("install", c =>
            {
                c.Description = "Installs a new Umbraco Db";

                var argConnString = c.Argument("[connectionstring]", "The database connection string");
                c.HelpOption("-h");

                c.OnExecuteAndReset(() =>
                {
                    ConnectDb(argConnString.Value, appContext);

                    System.Console.WriteLine("Validating schema...");
                    var schemaResult = appContext.DatabaseContext.ValidateDatabaseSchema();

                    if (schemaResult.ValidTables.Count > 0 || schemaResult.ValidColumns.Count > 0 || schemaResult.ValidConstraints.Count > 0 || schemaResult.ValidIndexes.Count > 0)
                    {
                        ConsoleHelper.WriteError("Database is already installed");
                    }
                    else
                    {
                        System.Console.WriteLine("Installing database...");
                        var installResult = appContext.DatabaseContext.CreateDatabaseSchemaAndData(appContext);
                        ConsoleHelper.WriteDictionaryVals(installResult.ToDictionary<object>());
                    }

                    return 0;
                });
            });
        }

        private static void ConnectDb(string connString, ApplicationContext appContext)
        {
            System.Console.WriteLine("Connecting to db...");

            var dbContext = appContext.DatabaseContext;

            //Example:
            //server=.\SQLExpress;database=UmbASPNetCore;user id=sa;password=test
            if (connString.IsNullOrWhiteSpace())
            {
                throw new InvalidOperationException("No connection string specified");
            }
            dbContext.ConfigureDatabaseConnection(connString);

            if (dbContext.CanConnect == false)
            {
                System.Console.WriteLine("Cannot connect to the db with the connection string specified");
                return;
            }

            System.Console.WriteLine("Connected to db !");
        }

    }
}