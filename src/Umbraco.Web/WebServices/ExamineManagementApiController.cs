using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Examine;
using Examine.LuceneEngine;
using Examine.LuceneEngine.Providers;
using Examine.Providers;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Web.Search;
using Umbraco.Web.WebApi;
using Umbraco.Web.WebApi.Filters;
using TypeHelper = Umbraco.Core.TypeHelper;

namespace Umbraco.Web.WebServices
{
    [ValidateAngularAntiForgeryToken]
    public class ExamineManagementApiController : UmbracoAuthorizedApiController
    {
        /// <summary>
        /// Checks if the member internal index is consistent with the data stored in the database
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public bool CheckMembersInternalIndex()
        {
            var total = Services.MemberService.Count();

            var criteria = ExamineManager.Instance.SearchProviderCollection[Constants.Examine.InternalMemberSearcher]
                .CreateSearchCriteria().RawQuery("__IndexType:member");
            var totalIndexed = ExamineManager.Instance.SearchProviderCollection[Constants.Examine.InternalMemberSearcher].Search(criteria);

            return total == totalIndexed.TotalItemCount;
        }

        /// <summary>
        /// Checks if the media internal index is consistent with the data stored in the database
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public bool CheckMediaInternalIndex()
        {
            var total = Services.MediaService.Count();

            var criteria = ExamineManager.Instance.SearchProviderCollection[Constants.Examine.InternalSearcher]
                .CreateSearchCriteria().RawQuery("__IndexType:media");
            var totalIndexed = ExamineManager.Instance.SearchProviderCollection[Constants.Examine.InternalSearcher].Search(criteria);

            return total == totalIndexed.TotalItemCount;
        }

        /// <summary>
        /// Checks if the content internal index is consistent with the data stored in the database
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public bool CheckContentInternalIndex()
        {
            var total = Services.ContentService.Count();

            var criteria = ExamineManager.Instance.SearchProviderCollection[Constants.Examine.InternalSearcher]
                .CreateSearchCriteria().RawQuery("__IndexType:content");
            var totalIndexed = ExamineManager.Instance.SearchProviderCollection[Constants.Examine.InternalSearcher].Search(criteria);

            return total == totalIndexed.TotalItemCount;
        }

        /// <summary>
        /// Get the details for indexers
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ExamineIndexerModel> GetIndexerDetails()
        {
            return ExamineManager.Instance.IndexProviderCollection.Select(CreateModel).OrderBy(x =>
            {
                //order by name , but strip the "Indexer" from the end if it exists
                return x.Name.TrimEnd("Indexer");
            });
        }

        /// <summary>
        /// Get the details for searchers
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ExamineSearcherModel> GetSearcherDetails()
        {
            var model = new List<ExamineSearcherModel>(
                ExamineManager.Instance.SearchProviderCollection.Cast<BaseSearchProvider>().Select(searcher =>
                {
                    var indexerModel = new ExamineSearcherModel()
                    {
                        Name = searcher.Name
                    };
                    var props = TypeHelper.CachedDiscoverableProperties(searcher.GetType(), mustWrite: false)
                        //ignore these properties
                                          .Where(x => new[] {"Description"}.InvariantContains(x.Name) == false)
                                          .OrderBy(x => x.Name);
                    foreach (var p in props)
                    {
                        indexerModel.ProviderProperties.Add(p.Name, p.GetValue(searcher, null).ToString());
                    }
                    return indexerModel;
                }).OrderBy(x =>
                {
                    //order by name , but strip the "Searcher" from the end if it exists
                    return x.Name.TrimEnd("Searcher");
                }));
            return model;
        }

        public ISearchResults GetSearchResults(string searcherName, string query, string queryType)
        {
            if (queryType == null)
            {
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound));
            }
                

            if (query.IsNullOrWhiteSpace())
                return SearchResults.Empty();

            LuceneSearcher searcher;
            var msg = ValidateLuceneSearcher(searcherName, out searcher);
            if (msg.IsSuccessStatusCode)
            {
                if (queryType.InvariantEquals("text"))
                {
                    return searcher.Search(query, false);
                }
                if (queryType.InvariantEquals("lucene"))
                {
                    return searcher.Search(searcher.CreateSearchCriteria().RawQuery(query));
                }
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound));
            }
            throw new HttpResponseException(msg);            
        }

        /// <summary>
        /// Optimizes an index
        /// </summary>
        public HttpResponseMessage PostOptimizeIndex(string indexerName)
        {
            var valid = ValidateLuceneIndexer(indexerName, out var indexer);
            if (valid == false)
                throw new InvalidOperationException($"The indexer {indexerName} is not of type {typeof(LuceneIndexer)}");

            try
            {
                indexer.OptimizeIndex();
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"The index could not be optimized, most likely there is another thread currently writing to the index. Error: {ex}");
            }
        }

        /// <summary>
        /// Rebuilds the index
        /// </summary>
        /// <param name="indexerName"></param>
        /// <returns></returns>
        public ExamineRebuildModel PostRebuildIndex(string indexerName)
        {
            
            var valid = ValidateExistingIndexer(indexerName);
            if (valid == false)
                throw new InvalidOperationException($"No indexer found with name = {indexerName}");

            LogHelper.Info<ExamineManagementApiController>($"Rebuilding index '{indexerName}'");

            var indexer = ExamineManager.Instance.IndexProviderCollection[indexerName];

            if (indexer is LuceneIndexer luceneIndexer)
            {
                //remove it in case there's a handler there alraedy
                luceneIndexer.IndexOperationComplete -= Indexer_IndexOperationComplete;
                //now add a single handler
                luceneIndexer.IndexOperationComplete += Indexer_IndexOperationComplete;

                var cacheKey = "temp_indexing_op_" + indexer.Name;
                //put temp val in cache which is used as a rudimentary way to know when the indexing is done
                ApplicationContext.ApplicationCache.RuntimeCache.InsertCacheItem(cacheKey, () => "tempValue", TimeSpan.FromMinutes(5), isSliding: false);

                try
                {
                    luceneIndexer.RebuildIndex();
                    return new ExamineRebuildModel { IsLuceneIndex = true };
                }
                catch (LockObtainFailedException)
                {
                    //this will occur if the index is locked (which it should defo not be!) so in this case we'll forcibly unlock it and try again
                    try
                    {
                        IndexWriter.Unlock(luceneIndexer.GetLuceneDirectory());
                        indexer.RebuildIndex();
                        return new ExamineRebuildModel {IsLuceneIndex = true};
                    }
                    catch (Exception e)
                    {
                        LogHelper.Error<ExamineManagementApiController>("An error occurred rebuilding index", e);
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Error<ExamineManagementApiController>("An error occurred rebuilding index", ex);
                    throw;
                }
            }
            else
            {
                try
                {
                    indexer.RebuildIndex();
                    return new ExamineRebuildModel();
                }
                catch (Exception ex)
                {
                    LogHelper.Error<ExamineManagementApiController>("An error occurred rebuilding index", ex);
                    throw;
                }
            }
            
        }
        
        //static listener so it's not GC'd
        private static void Indexer_IndexOperationComplete(object sender, EventArgs e)
        {
            var indexer = (LuceneIndexer) sender;

            //ensure it's not listening anymore
            indexer.IndexOperationComplete -= Indexer_IndexOperationComplete;

            LogHelper.Info<ExamineManagementApiController>(string.Format("Rebuilding index '{0}' done, {1} items committed (can differ from the number of items in the index)", indexer.Name, indexer.CommitCount));

            var cacheKey = "temp_indexing_op_" + indexer.Name;
            ApplicationContext.Current.ApplicationCache.RuntimeCache.ClearCacheItem(cacheKey);
        }

        /// <summary>
        /// Check if the index has been rebuilt
        /// </summary>
        /// <param name="indexerName"></param>
        /// <returns></returns>
        /// <remarks>
        /// This is kind of rudimentary since there's no way we can know that the index has rebuilt, we 
        /// have a listener for the index op complete so we'll just check if that key is no longer there in the runtime cache
        /// </remarks>
        public ExamineIndexerModel PostCheckRebuildIndex(string indexerName)
        {
            var valid = ValidateLuceneIndexer(indexerName, out var indexer);
            if (valid == false)
                throw new InvalidOperationException($"The indexer {indexerName} is not of type {typeof(LuceneIndexer)}");

            var cacheKey = "temp_indexing_op_" + indexerName;
            var found = ApplicationContext.ApplicationCache.RuntimeCache.GetCacheItem(cacheKey);                
            //if its still there then it's not done
            return found != null
                ? null 
                : CreateModel(indexer);
        }

        /// <summary>
        /// Checks if the index is optimized
        /// </summary>
        /// <param name="indexerName"></param>
        /// <returns></returns>
        public ExamineIndexerModel PostCheckOptimizeIndex(string indexerName)
        {
            var valid = ValidateLuceneIndexer(indexerName, out var indexer);
            if (valid == false)
                throw new InvalidOperationException($"The indexer {indexerName} is not of type {typeof(LuceneIndexer)}");

            var isOptimized = indexer.IsIndexOptimized();
            return isOptimized == false
                ? null
                : CreateModel(indexer);
        }

        private ExamineIndexerModel CreateModel(BaseIndexProvider indexer)
        {
            var indexerModel = new ExamineIndexerModel()
            {
                IndexCriteria = indexer.IndexerData,
                Name = indexer.Name
            };
            
            var props = TypeHelper.CachedDiscoverableProperties(indexer.GetType(), mustWrite: false)
                //ignore these properties
                                  .Where(x => new[] {"IndexerData", "Description", "WorkingFolder"}.InvariantContains(x.Name) == false)
                                  .OrderBy(x => x.Name);
								  
            foreach (var p in props)
            {
                var val = p.GetValue(indexer, null);
                if (val == null)
                {
                    // Do not warn for new new attribute that is optional
                    if(string.Equals(p.Name, "DirectoryFactory", StringComparison.InvariantCultureIgnoreCase) == false)
                        LogHelper.Warn<ExamineManagementApiController>("Property value was null when setting up property on indexer: " + indexer.Name + " property: " + p.Name);

                    val = string.Empty;
                }
                indexerModel.ProviderProperties.Add(p.Name, val.ToString());
            }

            var indexExists = indexer.IndexExists();
            if (indexExists == false)
            {
                indexerModel.DocumentCount = 0;
                indexerModel.FieldCount = 0;
                indexerModel.IsOptimized = true;
                indexerModel.DeletionCount = 0;
                return indexerModel;
            }

            var isHealthy = SetHealthyValue(indexer, indexerModel);
            if (isHealthy == false)
                return indexerModel; //we cannot continue at this point

            if (indexer is IIndexStatistics statsIndexer)
            {
                indexerModel.DocumentCount = statsIndexer.GetDocumentCount();
                indexerModel.FieldCount = statsIndexer.GetFieldCount();
            }

            if (indexer is LuceneIndexer luceneIndexer)
            {
                indexerModel.IsLuceneIndex = true;
                indexerModel.IsOptimized = luceneIndexer.IsIndexOptimized();
                indexerModel.DeletionCount = luceneIndexer.GetDeletedDocumentsCount();
            }

            return indexerModel;
        }

        private bool SetHealthyValue(IIndexer index, ExamineIndexerModel indexerModel)
        {
            if (!(index is IIndexReadable luceneIndex))
            {
                indexerModel.IsHealthy = true;
                return true;
            }

            indexerModel.IsHealthy = luceneIndex.IsReadable(out var ex);
            if (indexerModel.IsHealthy == false)
            {
                //we cannot continue at this point
                indexerModel.Error = ex.ToString();
            }

            return indexerModel.IsHealthy;
        }

        private HttpResponseMessage ValidateLuceneSearcher(string searcherName, out LuceneSearcher searcher)
        {
            if (ExamineManager.Instance.SearchProviderCollection.Cast<BaseSearchProvider>().Any(x => x.Name == searcherName))
            {
                searcher = ExamineManager.Instance.SearchProviderCollection[searcherName] as LuceneSearcher;
                if (searcher == null)
                {
                    var response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                    response.Content = new StringContent($"The searcher {searcherName} is not of type {typeof(LuceneSearcher)}");
                    response.ReasonPhrase = "Wrong Searcher Type";
                    return response;
                }
                //return Ok!
                return Request.CreateResponse(HttpStatusCode.OK);
            }

            searcher = null;

            var response1 = Request.CreateResponse(HttpStatusCode.InternalServerError);
            response1.Content = new StringContent($"No searcher found with name = {searcherName}");
            response1.ReasonPhrase = "Searcher Not Found";
            return response1;
        }

        private bool ValidateLuceneIndexer(string indexerName, out LuceneIndexer indexer)
        {
            var r = ValidateExistingIndexer(indexerName);
            if (r == false)
            {
                indexer = null;
                return false;
            }

            indexer = ExamineManager.Instance.IndexProviderCollection[indexerName] as LuceneIndexer;
            return indexer != null;
        }

        private bool ValidateExistingIndexer(string indexerName)
        {
            return ExamineManager.Instance.IndexProviderCollection.Any(x => x.Name == indexerName);
        }
    }
}

