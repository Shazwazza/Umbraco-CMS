using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Examine;
using Examine.AzureSearch;
using Examine.LuceneEngine.Config;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using UmbracoExamine.DataServices;
using UmbracoExamine.Config;

namespace UmbracoExamine.AzureSearch
{
    public class UmbracoContentAzureIndexer : AzureSearchIndexer, IUmbracoIndexer
    {
        private readonly Lazy<IIndexDataService> _dataService;
        private const int PageSize = 10000;
        /// <summary>
        /// A type that defines the type of index for each Umbraco field (non user defined fields)
        /// Alot of standard umbraco fields shouldn't be tokenized or even indexed, just stored into lucene
        /// for retreival after searching.
        /// </summary>
        private static readonly StaticFieldCollection IndexFieldPolicies = StaticFieldCollection.CreateDefaultUmbracoContentIndexFieldPolicies();

        public UmbracoContentAzureIndexer()
        {
            _dataService = CreateDataService(ApplicationContext.Current.Services);
        }

        public UmbracoContentAzureIndexer(string indexName, string searchServiceName, string apiKey,
            string analyzer, IIndexCriteria indexerData, IIndexDataService dataService)
            : base(indexName, searchServiceName, apiKey, analyzer, indexerData, dataService)
        {
        }

        public override void Initialize(string name, NameValueCollection config)
        {
            config.ConfigureOptions(out var supportUnpublishedContent, out var supportProtectedContent);
            SupportProtectedContent = supportProtectedContent;
            SupportUnpublishedContent = supportUnpublishedContent;

            EnableDefaultEventHandler = true; //set to true by default
            if (bool.TryParse(config["enableDefaultEventHandler"], out var enabled))
            {
                EnableDefaultEventHandler = enabled;
            }

            base.Initialize(name, config);
        }

        public override void RebuildIndex()
        {
            if (CanInitialize())
                base.RebuildIndex();
        }
        protected override void IndexItems(string type, IEnumerable<IndexDocument> docs, Action<IEnumerable<IndexedNode>> batchComplete)
        {
            if (CanInitialize())
                base.IndexItems(type, docs, batchComplete);
        }
        protected override void IndexItem(string id, string type, IDictionary<string, string> values, Action onComplete)
        {
            if (CanInitialize())
                base.IndexItem(id, type, values, onComplete);
        }
        protected override void DeleteItem(string id, Action<KeyValuePair<string, string>> onComplete)
        {
            if (CanInitialize())
                base.DeleteItem(id, onComplete);
        }

        protected override void OnGatheringNodeData(IndexingNodeDataEventArgs e)
        {
            base.OnGatheringNodeData(e);
            this.AddIndexDataForContent(UmbracoDataService.ContentService, e);
        }

        /// <summary>
        /// Creates an IIndexCriteria object based on the indexSet passed in and our DataService
        /// </summary>
        /// <param name="indexSet"></param>
        /// <returns></returns>
        /// <remarks>
        /// If we cannot initialize we will pass back empty indexer data since we cannot read from the database
        /// </remarks>
        public override IIndexCriteria CreateIndexerData(IndexSet indexSet)
        {
            if (CanInitialize() == false)
                return base.CreateIndexerData(indexSet);

            var criteria = indexSet.ToIndexCriteria(UmbracoDataService, IndexFieldPolicies,
                new[]
                {
                    //make sure the special fields exist: __nodeName, __Path, __NodeTypeAlias, __Key, __Icon
                    new IndexField { Name = UmbracoContentIndexer.NodeNameFieldName },
                    new IndexField { Name = UmbracoContentIndexer.IndexPathFieldName },
                    new IndexField { Name = UmbracoContentIndexer.NodeTypeAliasFieldName },
                    new IndexField { Name = UmbracoContentIndexer.NodeKeyFieldName },
                    new IndexField { Name = UmbracoContentIndexer.IconFieldName },
                });
            return criteria;
        }

        protected override bool ValidateDocument(XElement node)
        {
            // Test for access if we're only indexing published content
            // return nothing if we're not supporting protected content and it is protected, and we're not supporting unpublished content
            if (SupportUnpublishedContent == false
                && SupportProtectedContent == false)
            {
                var nodeId = int.Parse(node.Attribute("id").Value);

                if (UmbracoDataService.ContentService.IsProtected(nodeId, node.Attribute("path").Value))
                {
                    return false;
                }
            }
            return base.ValidateDocument(node);
        }

        public bool EnableDefaultEventHandler { get; set; }

        /// <summary>
        /// Determines if the manager will call the indexing methods when content is saved or deleted as
        /// opposed to cache being updated.
        /// </summary>
        public bool SupportUnpublishedContent { get; private set; }

        /// <summary>
        /// By default this is false, if set to true then the indexer will include indexing content that is flagged as publicly protected.
        /// This property is ignored if SupportUnpublishedContent is set to true.
        /// </summary>
        public bool SupportProtectedContent { get; private set; }

        public override IIndexDataService DataService => _dataService == null ? base.DataService : _dataService.Value;

        public IDataService UmbracoDataService { get; } = new UmbracoDataService();

        /// <summary>
        /// Returns true if the Umbraco application is in a state that we can initialize the examine indexes
        /// </summary>
        /// <returns></returns>
        protected bool CanInitialize()
        {
            return ApplicationContext.Current.CanInitialize();
        }

        private Lazy<IIndexDataService> CreateDataService(ServiceContext services)
        {
            var contentService = new Lazy<UmbracoContentDataService>(() =>
                new UmbracoContentDataService(IndexerData, SupportUnpublishedContent, PageSize,
                    services.ContentService,
                    services.ContentTypeService,
                    services.DataTypeService,
                    services.UserService,
                    new EntityXmlSerializer(),
                    (element, s) => GetDataToIndex(element, s, null),
                    CancellationToken.None));

            var mediaService = new Lazy<UmbracoMediaDataService>(() =>
                new UmbracoMediaDataService(IndexerData, PageSize,
                    services.MediaService,
                    services.ContentTypeService,
                    (element, s) => GetDataToIndex(element, s, null),
                    CancellationToken.None));

            return new Lazy<IIndexDataService>(() => new UmbracoExamineDataService(contentService.Value, mediaService.Value));
        }

    }
}
