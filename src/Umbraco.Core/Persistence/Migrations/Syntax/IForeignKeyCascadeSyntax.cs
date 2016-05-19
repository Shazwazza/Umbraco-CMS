using System.Data;
using Umbraco.Core.Persistence.DatabaseModelDefinitions;

namespace Umbraco.Core.Persistence.Migrations.Syntax
{
    public interface IForeignKeyCascadeSyntax<TNext, TNextFk> : IFluentSyntax
        where TNext : IFluentSyntax
        where TNextFk : IFluentSyntax
    {
        TNextFk OnDelete(CascadeRule rule);
        TNextFk OnUpdate(CascadeRule rule);
        TNext OnDeleteOrUpdate(CascadeRule rule);
    }
}