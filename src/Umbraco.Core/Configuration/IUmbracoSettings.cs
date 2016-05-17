namespace Umbraco.Core.Configuration
{
    /// <summary>
    /// TODO: This is sort of temporary until we get the whole thing building
    /// </summary>
    public interface IUmbracoSettings
    {
        IConnectionString ConnectionString { get; }
        bool IsDebuggingEnabled { get; }
    }

    /// <summary>
    /// TODO: This is sort of temporary until we get the whole thing building
    /// </summary>
    public interface IConnectionString
    {
        string ConnectionString { get; }
        string ProviderName { get; }
    }
}