using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Examine;
using Examine.AzureSearch;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace UmbracoExamine.AzureSearch
{
    public class UmbracoContentAzureIndexer : AzureSearchIndexer
    {
        private readonly Lazy<IIndexDataService> _dataService;

        private const int PageSize = 10000;

        public UmbracoContentAzureIndexer()
        {
            var contentService = new Lazy<UmbracoContentDataService>(() =>
                new UmbracoContentDataService(IndexerData, SupportUnpublishedContent, PageSize,
                    ApplicationContext.Current.Services.ContentService,
                    ApplicationContext.Current.Services.ContentTypeService,
                    ApplicationContext.Current.Services.DataTypeService,
                    ApplicationContext.Current.Services.UserService,
                    new EntityXmlSerializer(),
                    (element, s) => GetDataToIndex(element, s, null),
                    CancellationToken.None));

            var mediaService = new Lazy<UmbracoMediaDataService>(() =>
                new UmbracoMediaDataService(IndexerData, PageSize,
                    ApplicationContext.Current.Services.MediaService,
                    ApplicationContext.Current.Services.ContentTypeService,
                    (element, s) => GetDataToIndex(element, s, null),
                    CancellationToken.None));

            _dataService = new Lazy<IIndexDataService>(() => new UmbracoExamineDataService(contentService.Value, mediaService.Value));
        }

        public UmbracoContentAzureIndexer(string indexName, string searchServiceName, string apiKey,
            string analyzer, IIndexCriteria indexerData, IIndexDataService dataService)
            : base(indexName, searchServiceName, apiKey, analyzer, indexerData, dataService)
        {
            //TODO: Pass in all of these requirements

            var contentService = new Lazy<UmbracoContentDataService>(() =>
                new UmbracoContentDataService(IndexerData, SupportUnpublishedContent, PageSize,
                    ApplicationContext.Current.Services.ContentService,
                    ApplicationContext.Current.Services.ContentTypeService,
                    ApplicationContext.Current.Services.DataTypeService,
                    ApplicationContext.Current.Services.UserService,
                    new EntityXmlSerializer(),
                    (element, s) => GetDataToIndex(element, s, null),
                    CancellationToken.None));

            var mediaService = new Lazy<UmbracoMediaDataService>(() =>
                new UmbracoMediaDataService(IndexerData, PageSize,
                    ApplicationContext.Current.Services.MediaService,
                    ApplicationContext.Current.Services.ContentTypeService,
                    (element, s) => GetDataToIndex(element, s, null),
                    CancellationToken.None));

            _dataService = new Lazy<IIndexDataService>(() => new UmbracoExamineDataService(contentService.Value, mediaService.Value));
        }

        /// <summary>
        /// Determines if the manager will call the indexing methods when content is saved or deleted as
        /// opposed to cache being updated.
        /// </summary>
        public bool SupportUnpublishedContent { get; protected internal set; }

        public override IIndexDataService DataService => _dataService.Value;
    }
}
