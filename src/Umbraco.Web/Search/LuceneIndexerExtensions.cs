﻿using System;
using System.Linq;
using Examine;
using Examine.LuceneEngine.Providers;
using Examine.Providers;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Umbraco.Core.Logging;

namespace Umbraco.Web.Search
{
    /// <summary>
    /// Extension methods for the LuceneIndexer
    /// </summary>
    internal static class ExamineExtensions
    {

        /// <summary>
        /// Returns true if the index is optimized or not
        /// </summary>
        /// <param name="indexer"></param>
        /// <returns></returns>
        public static bool IsIndexOptimized(this LuceneIndexer indexer)
        {
            try
            {
                using (var reader = indexer.GetIndexWriter().GetReader())
                {
                    return reader.IsOptimized();
                }
            }
            catch (AlreadyClosedException)
            {
                LogHelper.Warn(typeof(ExamineExtensions), "Cannot get IsIndexOptimized, the writer is already closed");
                return false;
            }
        }

        /// <summary>
        /// Check if the index is locked
        /// </summary>
        /// <param name="indexer"></param>
        /// <returns></returns>
        /// <remarks>
        /// If the index does not exist we'll consider it locked
        /// </remarks>
        public static bool IsIndexLocked(this LuceneIndexer indexer)
        {   
            return indexer.IndexExists() == false
                   || IndexWriter.IsLocked(indexer.GetLuceneDirectory());
        }

        /// <summary>
        /// The number of documents deleted in the index
        /// </summary>
        /// <param name="indexer"></param>
        /// <returns></returns>
        public static int GetDeletedDocumentsCount(this LuceneIndexer indexer)
        {
            try
            {
                using (var reader = indexer.GetIndexWriter().GetReader())
                {
                    return reader.NumDeletedDocs();
                }
            }
            catch (AlreadyClosedException)
            {
                LogHelper.Warn(typeof(ExamineExtensions), "Cannot get GetDeletedDocumentsCount, the writer is already closed");
                return 0;
            }
        }
    }
}
