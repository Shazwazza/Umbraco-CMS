using System.Threading.Tasks;

namespace Umbraco.Web.Routing
{
    public interface IContentFinder
    {
        Task<bool> TryFindContentAsync(PublishedContentRequest pcr);
    }
}