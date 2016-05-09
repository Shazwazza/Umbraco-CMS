﻿using System.Data.Common;

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
}