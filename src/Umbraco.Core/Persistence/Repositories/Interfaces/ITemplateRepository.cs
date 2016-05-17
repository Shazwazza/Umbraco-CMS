using System;
using System.Collections.Generic;
using System.ComponentModel;
using Umbraco.Core.Models;

namespace Umbraco.Core.Persistence.Repositories
{
    public interface ITemplateRepository : IRepositoryQueryable<int, ITemplate>
    {
        ITemplate Get(string alias);

        IEnumerable<ITemplate> GetAll(params string[] aliases);

        IEnumerable<ITemplate> GetChildren(int masterTemplateId);
        IEnumerable<ITemplate> GetChildren(string alias);

        IEnumerable<ITemplate> GetDescendants(int masterTemplateId);
        IEnumerable<ITemplate> GetDescendants(string alias);

        /// <summary>
        /// Returns a template as a template node which can be traversed (parent, children)
        /// </summary>
        /// <param name="alias"></param>
        /// <returns></returns>
        [Obsolete("Use GetDescendants instead")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        TemplateNode GetTemplateNode(string alias);

        /// <summary>
        /// Given a template node in a tree, this will find the template node with the given alias if it is found in the hierarchy, otherwise null
        /// </summary>
        /// <param name="anyNode"></param>
        /// <param name="alias"></param>
        /// <returns></returns>
        [Obsolete("Use GetDescendants instead")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        TemplateNode FindTemplateInTree(TemplateNode anyNode, string alias);

        /// <summary>
        /// Validates a <see cref="ITemplate"/>
        /// </summary>
        /// <param name="template"><see cref="ITemplate"/> to validate</param>
        /// <returns>True if Script is valid, otherwise false</returns>
        bool ValidateTemplate(ITemplate template);
    }
}