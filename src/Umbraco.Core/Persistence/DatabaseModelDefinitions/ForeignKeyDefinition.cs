using System.Collections.Generic;
using System.Data;

namespace Umbraco.Core.Persistence.DatabaseModelDefinitions
{
    public class ForeignKeyDefinition
    {
        public ForeignKeyDefinition()
        {
            ForeignColumns = new List<string>();
            PrimaryColumns = new List<string>();
            //Set to None by Default
            OnDelete = CascadeRule.None;
            OnUpdate = CascadeRule.None;
        }

        public virtual string Name { get; set; }
        public virtual string ForeignTable { get; set; }
        public virtual string ForeignTableSchema { get; set; }
        public virtual string PrimaryTable { get; set; }
        public virtual string PrimaryTableSchema { get; set; }
        public virtual CascadeRule OnDelete { get; set; }
        public virtual CascadeRule OnUpdate { get; set; }
        public virtual ICollection<string> ForeignColumns { get; set; }
        public virtual ICollection<string> PrimaryColumns { get; set; }
    }
}