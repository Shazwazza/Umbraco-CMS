using System;
using ConsoleTables.Core;
using Umbraco.Core;
using Microsoft.Extensions.CommandLineUtils;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using System.Linq;

namespace Umbraco.Test.Console
{
    public static class ContentTypeCommands
    {
        public static void UseMediaTypeCommand(this CommandLineApplication app, ApplicationContext appContext)
        {
            app.Command("medtype", c =>
            {
                c.ContentTypeOptions("media", appContext.Services.MediaTypeService, appContext.Services.DataTypeService);                
                c.UseContentTypeCreateCommand("media", appContext.Services.MediaTypeService,
                    (name, alias) => new MediaType(-1)
                    {
                        Name = name,
                        Alias = alias
                    });                
            });
        }

        public static void UseDocumentTypeCommand(this CommandLineApplication app, ApplicationContext appContext)
        {
            app.Command("doctype", c =>
            {
                c.ContentTypeOptions("document", appContext.Services.ContentTypeService, appContext.Services.DataTypeService);
                c.UseContentTypeCreateCommand("document", appContext.Services.ContentTypeService,
                    (name, alias) => new ContentType(-1)
                    {
                        Name = name,
                        Alias = alias
                    });                
            });
        }

        private static void UseContentTypeCreateGroupCommand<TContentType>(this CommandLineApplication app,
            string ctTypeName,
            IContentTypeServiceBase<TContentType> ctService)
            where TContentType : IContentTypeComposition
        {
            app.Command("create", c =>
            {
                c.Description = $"Creates an Umbraco {ctTypeName} type group";                
                var argId = c.Argument("[alias|guid|id]", $"The Alias,Id or Guid of the {ctTypeName} type");
                var argName = c.Argument("[name]", $"The name of the {ctTypeName} type group");
                c.HelpOption("-h");

                c.OnExecuteAndReset(() =>
                {
                    var ct = GetContentTypeByStringId(argId.Value, ctService);
                    if (ct == null)
                    {
                        System.Console.WriteLine($"No {ctTypeName} found");
                        return 0;
                    }

                    var name = argName.Value;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        System.Console.WriteLine("No name specified");
                        return 0;
                    }

                    if (ct.CompositionPropertyGroups.Any(x => x.Name.InvariantEquals(name)))
                    {
                        System.Console.WriteLine($"Property group {name} already exists on {ctTypeName} type {ct.Alias}");
                        return 0;
                    }

                    System.Console.WriteLine($"Creating {ctTypeName} type group: {name}");
                    if (ct.AddPropertyGroup(name) == false)
                    {
                        System.Console.WriteLine($"Could not create property group {name}");
                        return 0;
                    }

                    ctService.Save(ct);
                    System.Console.WriteLine($"Created group: {name}, group Id: {ct.PropertyGroups[name].Id}");
                    return 1;
                });
            });
        }

        private static void UseContentTypeCreatePropertyCommand<TContentType>(this CommandLineApplication app,
            string ctTypeName,
            IContentTypeServiceBase<TContentType> ctService, IDataTypeService dtService)
            where TContentType : IContentTypeComposition
        {
            app.Command("create", c =>
            {
                c.Description = $"Creates an Umbraco {ctTypeName} type property";
                var argId = c.Argument("[alias|guid|id]", $"The Alias,Id or Guid of the {ctTypeName} type");
                var argName = c.Argument("[name]", $"The name of the {ctTypeName} type property");
                var argAlias = c.Argument("[alias]", $"The alias of the {ctTypeName} type property");
                var argGroup = c.Argument("[group]", $"The name or Id of the {ctTypeName} type group");
                var argDt = c.Argument("[datatype]", $"The name or Id of the Data Type");

                c.HelpOption("-h");

                c.OnExecuteAndReset(() =>
                {
                    var ct = GetContentTypeByStringId(argId.Value, ctService);
                    if (ct == null)
                    {
                        System.Console.WriteLine($"No {ctTypeName} found");
                        return 0;
                    }
                    if (string.IsNullOrWhiteSpace(argGroup.Value))
                    {
                        System.Console.WriteLine("No group specified");
                        return 0;
                    }
                    if (string.IsNullOrWhiteSpace(argDt.Value))
                    {
                        System.Console.WriteLine("No Data Type specified");
                        return 0;
                    }
                    var dt = GetDataTypeByStringId(argDt.Value, dtService);
                    if (dt == null)
                    {
                        System.Console.WriteLine($"No Data Type found by name or id {argDt.Value}");
                        return 0;
                    }

                    var name = argName.Value;
                    var alias = argAlias.Value;
                    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(alias))
                    {
                        System.Console.WriteLine("The name and alias must be specified");
                        return 0;
                    }

                    if (ct.CompositionPropertyTypes.Any(x => x.Alias.InvariantEquals(alias)))
                    {
                        System.Console.WriteLine($"Property type {alias} already exists on {ctTypeName} type {ct.Alias}");
                        return 0;
                    }

                    System.Console.WriteLine($"Creating {ctTypeName} type property: {alias}");

                    var pt = new PropertyType(dt, alias)
                    {
                        Name = name
                    };
                    if (ct.AddPropertyType(pt, argGroup.Value) == false)
                    {
                        System.Console.WriteLine($"Could not create property type {name}");
                    }
                    else
                    {
                        ctService.Save(ct);
                        System.Console.WriteLine($"Created property: {alias}, property Id: {pt.Id}");
                    }
                    return 1;
                });
            });
        }

        private static void UseContentTypeListGroupsCommand<TContentType>(this CommandLineApplication app,
           string ctTypeName,
           IContentTypeServiceBase<TContentType> ctService)
           where TContentType : IContentTypeComposition
        {
            app.Command("list", c =>
            {
                c.Description = $"Lists all {ctTypeName} type group objects";
                var argId = c.Argument("[alias|guid|id]", $"The Alias,Id or Guid of the {ctTypeName} type");
                var optionShowComps = c.Option("-c|--compositions", "Show property group compositions", CommandOptionType.NoValue);
                c.HelpOption("-h");

                c.OnExecuteAndReset(() =>
                {
                    var ct = GetContentTypeByStringId(argId.Value, ctService);
                    if (ct == null)
                    {
                        System.Console.WriteLine($"No {ctTypeName} found");
                        return 0;
                    }

                    var table = new ConsoleTable("Id", "Name", "Properties");

                    var result = optionShowComps.HasValue() ? ct.CompositionPropertyGroups.ToList() : ct.PropertyGroups.ToList();
                    foreach (var propertyGroup in result)
                    {                            
                        table.AddRow(propertyGroup.Id, propertyGroup.Name,
                            string.Join(", ", propertyGroup.PropertyTypes.Select(x => x.Alias)));
                    }
                    table.Write();
                    System.Console.WriteLine();
                    
                    return 1;
                });
            });
        }

        private static void UseContentTypeListPropertiesCommand<TContentType>(this CommandLineApplication app,
           string ctTypeName,
           IContentTypeServiceBase<TContentType> ctService)
           where TContentType : IContentTypeComposition
        {
            app.Command("list", c =>
            {
                c.Description = $"Lists all {ctTypeName} type property objects";
                var argId = c.Argument("[alias|guid|id]", $"The Alias,Id or Guid of the {ctTypeName} type");
                var optionShowComps = c.Option("-c|--compositions", "Show property type compositions", CommandOptionType.NoValue);
                c.HelpOption("-h");

                c.OnExecuteAndReset(() =>
                {
                    var ct = GetContentTypeByStringId(argId.Value, ctService);
                    if (ct == null)
                    {
                        System.Console.WriteLine($"No {ctTypeName} found");
                        return 0;
                    }

                    var table = new ConsoleTable("Id", "Alias", "Group", "Data Type", "Editor");

                    var result = optionShowComps.HasValue() ? ct.CompositionPropertyTypes.ToList() : ct.PropertyTypes.ToList();
                    foreach (var propertyType in result)
                    {
                        var group = ct.CompositionPropertyGroups.FirstOrDefault(x => x.PropertyTypes.Contains(propertyType.Alias));
                        table.AddRow(propertyType.Id, propertyType.Alias, group != null ? group.Name : string.Empty, 
                            propertyType.DataTypeDefinitionId, propertyType.PropertyEditorAlias);
                    }
                    table.Write();
                    System.Console.WriteLine();

                    return 1;
                });
            });
        }

        private static void UseContentTypeGroupsCommand<TContentType>(this CommandLineApplication app,
            string ctTypeName,
            IContentTypeServiceBase<TContentType> ctService)
            where TContentType : IContentTypeComposition
        {
            app.Command("groups", c =>
            {
                c.Description = $"Commands for working with the Umbraco {ctTypeName} type group objects";                
                c.HelpOption("-h");
                c.UseBackCommand();
                c.UseContentTypeCreateGroupCommand(ctTypeName, ctService);            
                c.UseContentTypeListGroupsCommand(ctTypeName ,ctService);
                c.OnExecute(() =>
                {
                    c.Prompt();
                    return 1;
                });
            });
        }

        private static void UseContentTypePropertyCommand<TContentType>(this CommandLineApplication app,
            string ctTypeName,
            IContentTypeServiceBase<TContentType> ctService, IDataTypeService dtService)
            where TContentType : IContentTypeComposition
        {
            app.Command("props", c =>
            {
                c.Description = $"Commands for working with the Umbraco {ctTypeName} type property objects";
                c.HelpOption("-h");
                c.UseBackCommand();
                c.UseContentTypeCreatePropertyCommand(ctTypeName, ctService, dtService);
                c.UseContentTypeListPropertiesCommand(ctTypeName, ctService);
                c.OnExecute(() =>
                {
                    c.Prompt();
                    return 1;
                });
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
                var argName = c.Argument("[name]", $"The name of the {ctTypeName} type");
                var argAlias = c.Argument("[alias]", $"The alias of the {ctTypeName} type");
                c.HelpOption("-h");                

                c.OnExecuteAndReset(() =>
                {
                    var name = argName.Value;
                    var alias = argAlias.Value;

                    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(alias))
                    {
                        System.Console.WriteLine("name and alias must be specified");
                        return 0;
                    }

                    System.Console.WriteLine($"Creating {ctTypeName} type: {name}, Alias: {alias}");
                    var ct = create(name, alias);
                    ctService.Save(ct);
                    System.Console.WriteLine($"Created! Id: {ct.Id}");

                    return 1;
                });
            });
        }

        private static IDataTypeDefinition GetDataTypeByStringId(string id, IDataTypeService dtService)
        {
            IDataTypeDefinition dt;
            int intId;
            if (int.TryParse(id, out intId))
            {
                dt = dtService.GetDataTypeDefinitionById(intId);
            }            
            else
            {
                dt = dtService.GetDataTypeDefinitionByName(id);
            }
            return dt;
        }

        private static TContentType GetContentTypeByStringId<TContentType>(string id, IContentTypeServiceBase<TContentType> ctService)
            where TContentType : IContentTypeComposition
        {
            TContentType ct;
            int intId;
            Guid guidId;
            if (int.TryParse(id, out intId))
            {
                ct = ctService.Get(intId);
            }
            else if (Guid.TryParse(id, out guidId))
            {
                ct = ctService.Get(guidId);
            }
            else
            {
                ct = ctService.Get(id);
            }
            return ct;
        }

        private static void UseContentTypeDeleteCommand<TContentType>(this CommandLineApplication app,
            string ctTypeName,
            IContentTypeServiceBase<TContentType> ctService)
            where TContentType : IContentTypeComposition
        {
            app.Command("del", c =>
            {
                c.Description = $"Deletes an Umbraco {ctTypeName} type objects";                               
                var argId = c.Argument("[alias|guid|id]", $"The Alias,Id or Guid of the {ctTypeName} type");
                c.HelpOption("-h");

                c.OnExecuteAndReset(() =>
                {
                    var ct = GetContentTypeByStringId(argId.Value, ctService);

                    if (ct == null)
                    {
                        System.Console.WriteLine($"No {ctTypeName} found");
                        return 0;
                    }

                    System.Console.WriteLine($"Deleting {ctTypeName} type: {ct.Alias}");
                    ctService.Delete(ct);
                    System.Console.WriteLine($"Deleting! Id: {ct.Id}");

                    return 1;
                });
            });
        }

        private static void UseContentTypeListCommand<TContentType>(this CommandLineApplication app,
            string ctTypeName,
            IContentTypeServiceBase<TContentType> ctService)
            where TContentType : IContentTypeComposition
        {
            app.Command("list", c =>
            {
                c.Description = $"Lists all {ctTypeName} type objects";
                c.OnExecute(() =>
                {
                    var table = new ConsoleTable("Id", "Name", "Alias");

                    var result = ctService.GetAll();
                    foreach (var dt in result)
                    {
                        table.AddRow(dt.Id, dt.Name, dt.Alias);
                    }
                    table.Write();
                    System.Console.WriteLine();

                    return 1;
                });
            });            
        }

        /// <summary>
        /// Sets default commands for content types
        /// </summary>
        /// <typeparam name="TContentType"></typeparam>
        /// <param name="app"></param>
        /// <param name="ctTypeName"></param>
        /// <param name="ctService"></param>
        /// <param name="dtService"></param>
        private static void ContentTypeOptions<TContentType>(this CommandLineApplication app, string ctTypeName, IContentTypeServiceBase<TContentType> ctService, IDataTypeService dtService)
            where TContentType : IContentTypeComposition
        {
            app.Description = $"Commands for working with the Umbraco {ctTypeName} type objects";
            app.HelpOption("-h");
            app.UseBackCommand();
            app.UseContentTypeListCommand(ctTypeName, ctService);
            app.UseContentTypeDeleteCommand(ctTypeName, ctService);
            app.UseContentTypeGroupsCommand(ctTypeName, ctService);
            app.UseContentTypePropertyCommand(ctTypeName, ctService, dtService);
            app.OnExecute(() =>
            {
                app.Prompt();
                return 1;
            });
        }
    }
}