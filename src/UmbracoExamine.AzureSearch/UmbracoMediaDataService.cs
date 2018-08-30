using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Examine;
using Umbraco.Core;
using Umbraco.Core.Services;

namespace UmbracoExamine.AzureSearch
{
    public class UmbracoMediaDataService
    {
        private readonly int _pageSize;
        private readonly IIndexCriteria _indexerData;
        private readonly IMediaService _mediaService;
        private readonly IContentTypeService _contentTypeService;
        private readonly Func<XElement, string, Dictionary<string, string>> _translator;
        private readonly CancellationToken _cancellationToken;
        private static readonly (int, XElement)[] EmptyXElement = new(int, XElement)[0];


        public UmbracoMediaDataService(
            IIndexCriteria indexerData,
            int pageSize,
            IMediaService mediaService,
            IContentTypeService contentTypeService,
            Func<XElement, string, Dictionary<string, string>> translator,
            CancellationToken cancellationToken)
        {
            _pageSize = pageSize;
            _indexerData = indexerData;
            _mediaService = mediaService;
            _contentTypeService = contentTypeService;
            _translator = translator;
            _cancellationToken = cancellationToken;
        }

        private (IEnumerable<(int, XElement)>, bool) GetPagedXmlEntries(string path, int pIndex, int pSize)
        {
            var result = _mediaService.GetPagedXmlEntries(path, pIndex, pSize, out _).ToList();
            var more = result.Count == pSize;

            return (result.Select(x =>
            {
                var id = x.AttributeValue<int>("id");
                return (id, x);
            }), more);
        }

        private IEnumerable<IndexDocument> GetAllDataFromXml(string indexType, int parentId)
        {
            var pageIndex = 0;

            var contentTypes = _contentTypeService.GetAllContentTypes().ToList();
            var icons = contentTypes.ToDictionary(x => x.Id, y => y.Icon);
            var parent = parentId == -1 ? null : _mediaService.GetById(parentId);
            bool more;

            do
            {
                IEnumerable<(int, XElement)> xmlEntry;

                if (parentId == -1)
                {
                    var pagedElements = GetPagedXmlEntries("-1", pageIndex, _pageSize);
                    xmlEntry = pagedElements.Item1;
                    more = pagedElements.Item2;
                }
                else if (parent == null)
                {
                    xmlEntry = EmptyXElement;
                    more = false;
                }
                else
                {
                    var pagedElements = GetPagedXmlEntries(parent.Path, pageIndex, _pageSize);
                    xmlEntry = pagedElements.Item1;
                    more = pagedElements.Item2;
                }

                //if specific types are declared we need to post filter them
                //TODO: Update the service layer to join the cmsContentType table so we can query by content type too
                if (_indexerData.IncludeNodeTypes.Any())
                {
                    var includeNodeTypeIds = contentTypes.Where(x => _indexerData.IncludeNodeTypes.Contains(x.Alias)).Select(x => x.Id);
                    xmlEntry = xmlEntry.Where(elm => includeNodeTypeIds.Contains(elm.Item2.AttributeValue<int>("nodeType"))).ToArray();
                }

                foreach (var entry in xmlEntry)
                {
                    var element = entry.Item2;
                    if (element.Attribute("icon") == null)
                    {
                        element.Add(new XAttribute("icon", icons[element.AttributeValue<int>("nodeType")]));
                    }

                    yield return new IndexDocument(entry.Item1, indexType, _translator(entry.Item2, indexType));
                }

                pageIndex++;
            } while (more && _cancellationToken.IsCancellationRequested == false); //don't continue if the app is shutting down

        }

        public IEnumerable<IndexDocument> GetAllData(string indexType)
        {
            var parentId = -1;
            if (_indexerData.ParentNodeId.HasValue && _indexerData.ParentNodeId.Value > 0)
            {
                parentId = _indexerData.ParentNodeId.Value;
            }

            return GetAllDataFromXml(indexType, parentId);
        }
    }
}
