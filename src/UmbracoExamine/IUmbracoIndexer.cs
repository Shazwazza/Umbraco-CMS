using Examine;

namespace UmbracoExamine
{
    /// <summary>
    /// A Marker interface for defining an Umbraco indexer
    /// </summary>
    public interface IUmbracoIndexer : IIndexer
    {
        bool EnableDefaultEventHandler { get; }
        bool SupportUnpublishedContent { get; }
    }
}
