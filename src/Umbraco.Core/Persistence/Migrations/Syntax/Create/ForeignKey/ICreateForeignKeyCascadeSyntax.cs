using System.Data;
using Umbraco.Core.Persistence.DatabaseModelDefinitions;

namespace Umbraco.Core.Persistence.Migrations.Syntax.Create.ForeignKey
{
    public interface ICreateForeignKeyCascadeSyntax : IFluentSyntax
    {
        ICreateForeignKeyCascadeSyntax OnDelete(CascadeRule rule);
        ICreateForeignKeyCascadeSyntax OnUpdate(CascadeRule rule);
        void OnDeleteOrUpdate(CascadeRule rule);
    }
}