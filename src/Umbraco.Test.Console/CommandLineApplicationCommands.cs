using System;
using System.Collections.Generic;
using ConsoleTables.Core;
using Microsoft.Extensions.CommandLineUtils;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Umbraco.Test.Console
{
    /// <summary>
    /// Extension methods used to create commands
    /// </summary>
    public static class CommandLineApplicationCommands
    {
        private static void ContentTypeOptions<TContentType>(this CommandLineApplication c, 
            string ctTypeName, 
            IContentTypeServiceBase<TContentType> ctService, 
            Func<int> onExecute = null) 
            where TContentType : IContentTypeComposition
        {
            c.Description = $"Commands for working with the Umbraco {ctTypeName} type objects";            
            var optionList = c.Option("-l|--list", $"Lists the available {ctTypeName} types", CommandOptionType.NoValue);            
            c.HelpOption("-h");
            c.UseBackCommand();
            c.OnExecute(() =>
            {
                if (optionList.HasValue())
                {
                    var table = new ConsoleTable("Id", "Name", "Alias");

                    var result = ctService.GetAll();
                    foreach (var dt in result)
                    {
                        table.AddRow(dt.Id, dt.Name, dt.Alias);
                    }
                    table.Write();
                    System.Console.WriteLine();
                }

                if (onExecute != null)
                    return onExecute();

                c.Prompt();

                return 0;
            });
        }

        public static void UseMediaTypeCommand(this CommandLineApplication app, ApplicationContext appContext)
        {
            app.Command("medtype", c =>
            {                
                c.ContentTypeOptions("media", appContext.Services.MediaTypeService);
                c.UseContentTypeCreateCommand("media", appContext.Services.MediaTypeService,
                    (name, alias) => new MediaType(-1)
                    {
                        Name = name,
                        Alias = alias
                    });
            });
        }

        public static void UseContentTypeCreateCommand<TContentType>(this CommandLineApplication app,
            string ctTypeName,
            IContentTypeServiceBase<TContentType> ctService,             
            Func<string, string, TContentType> create)
            where TContentType : IContentTypeComposition
        {
            app.Command("create", c =>
            {
                c.Description = $"Creates an Umbraco {ctTypeName} type objects";
                var optionName = c.Option("-n|--name <NAME>", $"The name of the {ctTypeName} type", CommandOptionType.SingleValue);
                var optionAlias = c.Option("-a|--alias <ALIAS>", $"The alias of the {ctTypeName} type", CommandOptionType.SingleValue);
                c.HelpOption("-h");

                c.OnExecute(() =>
                {
                    var name = optionName.Value();
                    var alias = optionAlias.Value();

                    System.Console.WriteLine($"Creating {ctTypeName} type: {name}, Alias: {alias}");
                    var ct = create(name, alias);
                    ctService.Save(ct);
                    System.Console.WriteLine($"Created! Id: {ct.Id}");
                    return 0;
                });
            });
        }

        public static void UseDocumentTypeCommand(this CommandLineApplication app, ApplicationContext appContext)
        {
            app.Command("doctype", c =>
            {
                c.ContentTypeOptions("document", appContext.Services.MediaTypeService);
                c.UseContentTypeCreateCommand("document", appContext.Services.ContentTypeService,
                    (name, alias) => new ContentType(-1)
                    {
                        Name = name,
                        Alias = alias
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
                c.UseMediaTypeCommand(appContext);
                c.UseBackCommand();

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
                c.UseBackCommand();

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

        /// <summary>
        /// Moves the cursor back on level (or exits)
        /// </summary>
        /// <param name="app"></param>
        /// <param name="name"></param>
        /// <param name="desc"></param>
        public static void UseBackCommand(this CommandLineApplication app, string name = "back", string desc = "Moves the cursor back one level")
        {
            app.Command(name, c =>
            {
                c.Description = desc;
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