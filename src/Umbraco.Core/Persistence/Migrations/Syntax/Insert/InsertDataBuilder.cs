using System;
using System.Collections.Generic;
using System.ComponentModel;
using Umbraco.Core.Persistence.DatabaseModelDefinitions;
using Umbraco.Core.Persistence.Migrations.Syntax.Insert.Expressions;

namespace Umbraco.Core.Persistence.Migrations.Syntax.Insert
{
    public class InsertDataBuilder : IInsertDataSyntax
    {
         private readonly InsertDataExpression _expression;

         public InsertDataBuilder(InsertDataExpression expression)
        {
            _expression = expression;
        }

        public IInsertDataSyntax EnableIdentityInsert()
        {
            _expression.EnabledIdentityInsert = true;
            return this;
        }

        public IInsertDataSyntax Row(object dataAsAnonymousType)
        {
            _expression.Rows.Add(GetData(dataAsAnonymousType));
            return this;
        }

        private static InsertionDataDefinition GetData(object dataAsAnonymousType)
        {
            var data = new InsertionDataDefinition();
#if NET461
            var properties = TypeDescriptor.GetProperties(dataAsAnonymousType);

            foreach (PropertyDescriptor property in properties)
            {
                data.Add(new KeyValuePair<string, object>(property.Name, property.GetValue(dataAsAnonymousType)));
            }
#else
            throw new NotImplementedException("TODO: Fix GetData with RC2 since TypeDescriptor.GetProperties is supported");
#endif

            return data;
        }
    }
}