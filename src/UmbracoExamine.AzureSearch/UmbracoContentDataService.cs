using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Examine;
using Umbraco.Core;
using Umbraco.Core.Persistence.DatabaseModelDefinitions;
using Umbraco.Core.Services;

namespace UmbracoExamine.AzureSearch
{
    public class UmbracoContentDataService
    {
        private readonly int _pageSize;
        private readonly bool _supportUnpublishedContent;
        private readonly IIndexCriteria _indexerData;
        private readonly IContentService _contentService;
        private readonly IContentTypeService _contentTypeService;
        private readonly IDataTypeService _dataTypeService;
        private readonly IUserService _userService;
        private readonly EntityXmlSerializer _entityXmlSerializer;
        private readonly Func<XElement, string, Dictionary<string, string>> _translator;
        private readonly CancellationToken _cancellationToken;
        private static readonly (int, XElement)[] EmptyXElement = new (int, XElement)[0];


        public UmbracoContentDataService(
            IIndexCriteria indexerData,
            bool supportUnpublishedContent,
            int pageSize,
            IContentService contentService,
            IContentTypeService contentTypeService,
            IDataTypeService dataTypeService,
            IUserService userService,
            EntityXmlSerializer entityXmlSerializer,
            Func<XElement, string, Dictionary<string, string>> translator,
            CancellationToken cancellationToken)
        {
            _pageSize = pageSize;
            _supportUnpublishedContent = supportUnpublishedContent;
            _indexerData = indexerData;
            _contentService = contentService;
            _contentTypeService = contentTypeService;
            _dataTypeService = dataTypeService;
            _userService = userService;
            _entityXmlSerializer = entityXmlSerializer;
            _translator = translator;
            _cancellationToken = cancellationToken;
        }

        private (IEnumerable<(int, XElement)>, bool) GetPagedXmlEntries(string path, int pIndex, int pSize, IReadOnlyList<int> allNodesWithPublishedVersions, ref XElement last, ISet<int?> trackedIds)
        {
            //sorted by: umbracoNode.level, umbracoNode.parentID, umbracoNode.sortOrder
            var result = _contentService.GetPagedXmlEntries(path, pIndex, pSize, out _).ToList();
            var more = result.Count == pSize;

            //then like we do in the ContentRepository.BuildXmlCache we need to track what Parents have been processed
            // already so that we can then exclude implicitly unpublished content items
            var filtered = new List<(int, XElement)>();

            foreach (var xml in result)
            {
                var id = xml.AttributeValue<int>("id");

                //don't include this if it doesn't have a published version
                if (allNodesWithPublishedVersions.Contains(id) == false)
                    continue;

                var parentId = xml.AttributeValue<int?>("parentID");

                if (parentId == null) continue; //this shouldn't happen

                //if the parentid is changing
                if (last != null && last.AttributeValue<int?>("parentID") != parentId)
                {
                    var found = trackedIds.Contains(parentId);
                    if (found == false)
                    {
                        //Need to short circuit here, if the parent is not there it means that the parent is unpublished
                        // and therefore the child is not published either so cannot be included in the xml cache
                        continue;
                    }
                }

                last = xml;

                trackedIds.Add(id);

                filtered.Add((id, xml));
            }

            return (filtered, more);
        }

        private IEnumerable<IndexDocument> GetAllDataFromXml(string indexType, int parentId)
        {
            var pageIndex = 0;

            //get all node Ids that have a published version - this is a fail safe check, in theory
            // only document nodes that have a published version would exist in the cmsContentXml table
            var allNodesWithPublishedVersions = ApplicationContext.Current.DatabaseContext.Database.Fetch<int>(
                "select DISTINCT cmsDocument.nodeId from cmsDocument where cmsDocument.published = 1");

            XElement last = null;
            var trackedIds = new HashSet<int?>();

            var contentTypes = _contentTypeService.GetAllContentTypes().ToList();
            var icons = contentTypes.ToDictionary(x => x.Id, y => y.Icon);
            var parent = parentId == -1 ? null : _contentService.GetById(parentId);
            bool more;

            do
            {
                IEnumerable<(int, XElement)> xmlEntry;

                if (parentId == -1)
                {
                    var pagedElements = GetPagedXmlEntries("-1", pageIndex, _pageSize, allNodesWithPublishedVersions, ref last, trackedIds);
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
                    var pagedElements = GetPagedXmlEntries(parent.Path, pageIndex, _pageSize, allNodesWithPublishedVersions, ref last, trackedIds);
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

        private IEnumerable<IndexDocument> GetAllDataFromDb(string indexType, int parentId)
        {
            var pageIndex = 0;

            //used to track non-published entities so we can determine what items are implicitly not published
            //currently this is not in use apart form in tests
            var notPublished = new HashSet<string>();

            int currentPageSize;
            do
            {
                var descendants = _supportUnpublishedContent
                    ? _contentService.GetPagedDescendants(parentId, pageIndex, _pageSize, out long _, "umbracoNode.id").ToList()
                    : _contentService.GetPagedDescendants(parentId, pageIndex, _pageSize, out _, "level", Direction.Ascending, true, (string) null).ToList();

                // need to store decendants count before filtering, in order for loop to work correctly
                currentPageSize = descendants.Count;

                //if specific types are declared we need to post filter them
                //TODO: Update the service layer to join the cmsContentType table so we can query by content type too
                var content = _indexerData.IncludeNodeTypes.Any()
                    ? descendants.Where(x => _indexerData.IncludeNodeTypes.Contains(x.ContentType.Alias))
                    : descendants;

                foreach (var c in content)
                {
                    if (_supportUnpublishedContent == false)
                    {
                        //if we don't support published content and this is not published then track it and return null
                        if (c.Published == false)
                        {
                            notPublished.Add(c.Path);
                            //yield return null;
                            continue;
                        }

                        //if we don't support published content, check if this content item exists underneath any already tracked
                        //unpublished content and if so return null;
                        if (notPublished.Any(path => c.Path.StartsWith($"{path},")))
                        {
                            //yield return null;
                            continue;
                        }
                    }

                    var keyVals = _entityXmlSerializer.KeyVals(_dataTypeService, _userService, c);
                    var dic = new Dictionary<string, string>();
                    foreach (var kv in keyVals)
                    {
                        dic[kv.Key] = kv.Value.ToString();
                    }

                    var doc = new IndexDocument(c.Id, indexType, dic);
                    //add a custom 'icon' attribute
                    doc.RowData["icon"] = c.ContentType.Icon;

                    yield return doc;
                }

                pageIndex++;
            } while (currentPageSize == _pageSize && _cancellationToken.IsCancellationRequested == false); //do not continue if app is shutting down

        }

        public IEnumerable<IndexDocument> GetAllData(string indexType)
        {
            var parentId = -1;
            if (_indexerData.ParentNodeId.HasValue && _indexerData.ParentNodeId.Value > 0)
            {
                parentId = _indexerData.ParentNodeId.Value;
            }

            return _supportUnpublishedContent == false ? GetAllDataFromXml(indexType, parentId) : GetAllDataFromDb(indexType, parentId);
        }
    }
}
