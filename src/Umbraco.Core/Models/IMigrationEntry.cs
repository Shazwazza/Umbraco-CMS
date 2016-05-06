using Umbraco.Core.Models.EntityBase;

namespace Umbraco.Core.Models
{
    public interface IMigrationEntry : IAggregateRoot, IRememberBeingDirty
    {
        string MigrationName { get; set; }
        ISemVersion Version { get; set; }
    }
}