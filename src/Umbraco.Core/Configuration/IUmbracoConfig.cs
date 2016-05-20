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
        private readonly IConfiguration _config;

        public UmbracoConfigSection(IConfiguration configuration)
        {
            _config = configuration;
        }

        public IConnectionString ConnectionString
        {
            get
            {
                var connectionString = _config.GetSection("connectionString");
                if (connectionString == null) return null;
                return new ConnectionStringSection(connectionString);
            }
        }

        public bool IsDebuggingEnabled
        {
            get
            {
                bool debug;
                bool.TryParse(_config["isDebuggingEnabled"], out debug);
                return debug;
            }
        }

        public bool UseSSL
        {
            get
            {
                bool debug;
                bool.TryParse(_config["useSSL"], out debug);
                return debug;
            }
        }

        public string DefaultUILanguage => _config["defaultUILanguage"];

        public string ConfigurationStatus => _config["configurationStatus"];
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
        private readonly IConfiguration _config;

        public ConnectionStringSection(IConfiguration configuration)
        {
            _config = configuration;
        }

        public string ConnectionString => _config["connectionString"];

        public string ProviderName => _config["providerName"];

        public void Set(string connString, string providerName)
        {
            //TODO: Do we need to do something like this with a ConfigurationProvider?

            throw new NotImplementedException();
        }
    }
}