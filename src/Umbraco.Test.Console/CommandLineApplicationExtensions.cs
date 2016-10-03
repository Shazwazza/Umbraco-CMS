using System;
using Microsoft.Extensions.CommandLineUtils;
using Umbraco.Core;

namespace Umbraco.Test.Console
{
    public static class CommandLineApplicationExtensions
    {
        private static void ConnectDb(CommandOption optionConnString, ApplicationContext appContext)
        {
            System.Console.WriteLine("Connecting to db...");

            var dbContext = appContext.DatabaseContext;

            if (dbContext.IsDatabaseConfigured == false)
            {
                //Example:
                //server=.\SQLExpress;database=UmbASPNetCore;user id=sa;password=test
                var connString = optionConnString.Value();
                if (connString.IsNullOrWhiteSpace())
                {
                    throw new InvalidOperationException("No connection string specified");
                }
                dbContext.ConfigureDatabaseConnection(connString);
            }

            if (dbContext.CanConnect == false)
            {
                throw new InvalidOperationException("Cannot connect to the db with the connection string specified");
            }
            System.Console.WriteLine("Connected to db !");
        }

        public static void UseConnectCommand(this CommandLineApplication app, ApplicationContext appContext)
        {
            app.Command("db-connect", c =>
            {
                c.Description = "Connects to an existing Umbraco Db";
                var optionConnString = c.Option("-cs|--connectionstring <CONNECTIONSTRING>", "The database connection string", CommandOptionType.SingleValue);
                c.HelpOption("-h");

                c.OnExecute(() =>
                {
                    ConnectDb(optionConnString, appContext);

                    System.Console.WriteLine("Validating schema...");
                    var schemaResult = appContext.DatabaseContext.ValidateDatabaseSchema();

                    if (schemaResult.ValidTables.Count <= 0 && schemaResult.ValidColumns.Count <= 0 && schemaResult.ValidConstraints.Count <= 0 && schemaResult.ValidIndexes.Count <= 0)
                        throw new InvalidOperationException("The database is not installed, run db-install");                    

                    return 0;
                });
            });
        }

        public static void UseQuitCommand(this CommandLineApplication app)
        {
            app.Command("quit", c =>
            {
                c.Description = "Exits application";
                c.OnExecute(() => 101);
            });
        }

        public static void UseDbInstallCommand(this CommandLineApplication app, ApplicationContext appContext)
        {
            app.Command("db-install", c =>
            {
                c.Description = "Installs a new Umbraco Db";

                var optionConnString = c.Option("-cs|--connectionstring <CONNECTIONSTRING>", "The database connection string", CommandOptionType.SingleValue);

                c.HelpOption("-h");

                c.OnExecute(() =>
                {
                    ConnectDb(optionConnString, appContext);

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
    }
}