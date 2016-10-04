using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.PlatformAbstractions;

namespace Umbraco.Core.Configuration
{
    //TODO: We'll probably need something like this for:
    // * if we want to continue to just use the current xml config files -- probably
    // * if we want to write to the config source to save settings
    //public class UmbracoConfigurationProvider : ConfigurationProvider
    //{
        
    //}

    /// <summary>
    /// TODO: This is sort of temporary until we get the whole thing building
    /// </summary>
    public interface IUmbracoConfig
    {
        IConnectionString ConnectionString { get; }
        //TODO: This might not make sense, but maybe. We can also use the sProduction {get;}, IsStaging, and IsDevelopment extensions
        bool IsDebuggingEnabled { get; }
        bool UseSSL { get; }
        string DefaultUILanguage { get; }
        string ConfigurationStatus { get; }
    }

    public class UmbracoConfigSection : IUmbracoConfig
    {
        public UmbracoConfigSection()
        {
            //set defaults
            IsDebuggingEnabled = true;
            UseSSL = false;
            DefaultUILanguage = "en-US";
            ConnectionString = new ConnectionStringSection();
        }

        //bindable
        public ConnectionStringSection ConnectionString { get; set; }
        //explicit
        IConnectionString IUmbracoConfig.ConnectionString
        {
            get { return ConnectionString; }
        }

        public bool IsDebuggingEnabled { get; set; }

        public bool UseSSL { get; set; }

        public string DefaultUILanguage { get; set; }

        public string ConfigurationStatus { get; set; }
    }


    /// <summary>
    /// TODO: This is sort of temporary until we get the whole thing building
    /// </summary>
    public interface IConnectionString
    {
        //TODO: In aspnetcore this syntax could be used to extract the conn string: _connectionStringConfig[$"Data:{Constants.System.UmbracoConnectionName}:ConnectionString"];
        // see: https://docs.asp.net/en/latest/fundamentals/configuration.html
        string ConnectionString { get; }
        string ProviderName { get; }
        void Set(string connString, string providerName);
    }

    public class ConnectionStringSection : IConnectionString
    {
        public string ConnectionString { get; set; }

        public string ProviderName { get; set; }

        public void Set(string connString, string providerName)
        {
            ProviderName = providerName;
            ConnectionString = connString;
        }
    }
}