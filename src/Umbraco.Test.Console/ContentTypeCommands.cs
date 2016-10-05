using System;
using ConsoleTables.Core;
using Umbraco.Core;
using Microsoft.Extensions.CommandLineUtils;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Umbraco.Test.Console
{
    public static class ContentTypeCommands
    {
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
                c.UseContentTypeDeleteCommand("media", appContext.Services.ContentTypeService);
            });
        }

        public static void UseDocumentTypeCommand(this CommandLineApplication app, ApplicationContext appContext)
        {
            app.Command("doctype", c =>
            {
                c.ContentTypeOptions("document", appContext.Services.ContentTypeService);
                c.UseContentTypeCreateCommand("document", appContext.Services.ContentTypeService,
                    (name, alias) => new ContentType(-1)
                    {
                        Name = name,
                        Alias = alias
                    });
                c.UseContentTypeDeleteCommand("document", appContext.Services.ContentTypeService);
            });
        }

        private static void UseContentTypeCreateCommand<TContentType>(this CommandLineApplication app,
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

        private static void UseContentTypeDeleteCommand<TContentType>(this CommandLineApplication app,
            string ctTypeName,
            IContentTypeServiceBase<TContentType> ctService)
            where TContentType : class, IContentTypeComposition
        {
            app.Command("del", c =>
            {
                c.Description = $"Deletes an Umbraco {ctTypeName} type objects";               
                var optionAlias = c.Option("-a|--alias <ALIAS>", $"The alias of the {ctTypeName} type", CommandOptionType.SingleValue);
                var optionId = c.Option("-i|--id <ALIAS>", $"The id of the {ctTypeName} type", CommandOptionType.SingleValue);
                var optionGuid = c.Option("-g|--guid <ALIAS>", $"The guid of the {ctTypeName} type", CommandOptionType.SingleValue);
                c.HelpOption("-h");

                c.OnExecute(() =>
                {
                    TContentType ct = null;
                    if (optionAlias.HasValue())
                    {
                        ct = ctService.Get(optionAlias.Value());
                    }
                    else if (optionId.HasValue())
                    {
                        ct = ctService.Get(int.Parse(optionId.Value()));
                    }
                    else if (optionGuid.HasValue())
                    {
                        ct = ctService.Get(Guid.Parse(optionId.Value()));
                    }                    
                    if (ct != null)
                    {
                        System.Console.WriteLine($"Deleting {ctTypeName} type: {ct.Alias}");
                        ctService.Delete(ct);
                        System.Console.WriteLine($"Deleting! Id: {ct.Id}");
                    }
                    else
                    {
                        System.Console.WriteLine($"No {ctTypeName} found");
                    }
                    
                    return 0;
                });
            });
        }

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
    }
}