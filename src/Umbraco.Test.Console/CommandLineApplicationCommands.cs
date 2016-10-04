using System;
using Microsoft.Extensions.CommandLineUtils;
using Umbraco.Core;
using Umbraco.Core.Models;

namespace Umbraco.Test.Console
{
    /// <summary>
    /// Extension methods used to create commands
    /// </summary>
    public static class CommandLineApplicationCommands
    {
        public static void UseDocumentTypeCommand(this CommandLineApplication app, ApplicationContext appContext)
        {
            app.Command("dt", c =>
            {
                c.Description = "Commands for working with the Umbraco Document Type objects";
                
                var optionCreate = c.Option("-c|--create", "Creates a new content type", CommandOptionType.NoValue);
                var optionName = c.Option("-n|--name <NAME>", "The name of the content type", CommandOptionType.SingleValue);
                var optionAlias = c.Option("-a|--alias <ALIAS>", "The alias of the content type", CommandOptionType.SingleValue);
                c.HelpOption("-h");

                c.OnExecute(() =>
                {
                    if (optionCreate.HasValue())
                    {
                        var name = optionName.Value();
                        var alias = optionAlias.Value();

                        System.Console.WriteLine($"Creating doc type: {name}, Alias: {alias}");
                        var ct = new ContentType(-1)
                        {
                            Name = name,
                            Alias = alias
                        };
                        appContext.Services.ContentTypeService.Save(ct);
                        System.Console.WriteLine($"Created! Id: {ct.Id}");
                    }

                    return 0;
                });
            });
        }

        public static void UseSchemaCommand(this CommandLineApplication app, ApplicationContext appContext)
        {
            app.Command("schema", c =>
            {
                c.Description = "Commands for working with the Umbraco Schema objects";
                c.HelpOption("-h");

                c.UseDocumentTypeCommand(appContext);
                c.UseQuitCommand();

                c.OnExecute(() =>
                {
                    c.Prompt();

                    return 0;
                });
            });
        }

        public static void UseDbCommand(this CommandLineApplication app, ApplicationContext appContext)
        {
            app.Command("db", c =>
            {
                c.Description = "Commands for working with the Umbraco Db";

                c.UseDbInstallCommand(appContext);
                c.UseConnectCommand(appContext);
                c.UseQuitCommand();

                c.OnExecute(() =>
                {
                    c.Prompt();

                    return 0;
                });
            });
        }

        public static void UseConnectCommand(this CommandLineApplication app, ApplicationContext appContext)
        {
            app.Command("connect", c =>
            {
                c.Description = "Connects to an existing Umbraco Db";
                var argConnString = c.Argument("<CONNECTIONSTRING>", "The database connection string");
                c.HelpOption("-h");

                c.OnExecute(() =>
                {
                    ConnectDb(argConnString.Value, appContext);

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

                var argConnString = c.Argument("<CONNECTIONSTRING>", "The database connection string");

                c.HelpOption("-h");

                c.OnExecute(() =>
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

            if (dbContext.IsDatabaseConfigured == false)
            {
                //Example:
                //server=.\SQLExpress;database=UmbASPNetCore;user id=sa;password=test
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

    }
}