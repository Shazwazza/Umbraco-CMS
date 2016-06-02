using System;
using System.Threading.Tasks;
using Umbraco.Core.Models;
using Umbraco.Web.Models;

namespace Umbraco.Web.PublishedCache
{
    public interface IPublishedCache
    {
        Task<IPublishedContent> GetByIdAsync(bool preview, Guid contentId);
    }
}
