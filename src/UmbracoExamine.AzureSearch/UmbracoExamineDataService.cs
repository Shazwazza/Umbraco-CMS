using System;
using System.Collections.Generic;
using Examine;

namespace UmbracoExamine.AzureSearch
{
    public class UmbracoExamineDataService : IIndexDataService
    {
        private readonly UmbracoContentDataService _contentService;
        private readonly UmbracoMediaDataService _mediaService;

        public UmbracoExamineDataService(UmbracoContentDataService contentService, UmbracoMediaDataService mediaService)
        {
            _contentService = contentService;
            _mediaService = mediaService;
        }

        public IEnumerable<IndexDocument> GetAllData(string indexType)
        {
            switch (indexType)
            {
                case IndexTypes.Content:
                    return _contentService.GetAllData(indexType);
                case IndexTypes.Media:
                    return _mediaService.GetAllData(indexType);
                default:
                    throw new IndexOutOfRangeException("indexType not supported: " + indexType);
            }
        }
    }
}
