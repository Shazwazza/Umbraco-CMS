namespace Umbraco.Core.Configuration
{
    /// <summary>
    /// TODO: This is sort of temporary until we get the whole thing building
    /// </summary>
    public interface IUmbracoSettings
    {
        IConnectionString ConnectionString { get; }
        bool IsDebuggingEnabled { get; }
        bool UseSSL { get; }
        string DefaultUILanguage { get; }
        string ConfigurationStatus { get; }
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
}