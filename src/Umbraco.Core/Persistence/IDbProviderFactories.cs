using System;
using System.Data.Common;
using System.Data.SqlClient;

namespace Umbraco.Core.Persistence
{
    

    /// <summary>
    /// TODO: This is temporary until RC2 is out when the normal DBProviderFactories will be back: 
    /// https://github.com/dotnet/corefx/issues/6476
    /// https://github.com/dotnet/corefx/issues/4571
    /// </summary>
    internal interface IDbProviderFactories
    {
        DbProviderFactory GetFactory(string providerName);
    }

    internal class DefaultDbProviderFactories : IDbProviderFactories
    {
        public DbProviderFactory GetFactory(string providerName)
        {
            //DbFactories.

#if NET461
            return DbProviderFactories.GetFactory(providerName);
#else
            switch (providerName)
            {
                case Constants.DbProviderNames.MySql:
                    return MySql.Data.MySqlClient.MySqlClientFactory.Instance;
                case Constants.DbProviderNames.SqlServer:
                    return SqlClientFactory.Instance;
                default:
                    throw new NotSupportedException("Only MySql and SQL Server are supported on dotnetcore currently");
            }
            
            
#endif





        }
    }
}