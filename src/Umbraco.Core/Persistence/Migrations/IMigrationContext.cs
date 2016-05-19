using System.Collections.Generic;
using NPoco;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence.SqlSyntax;

namespace Umbraco.Core.Persistence.Migrations
{
    public interface IMigrationContext
    {
        UmbracoDatabase Database { get; }
        DatabaseType DatabaseType { get; }
        ICollection<IMigrationExpression> Expressions { get; set; }
        ILogger Logger { get; }
        ISqlSyntaxProvider SqlSyntax { get; }
    }
}