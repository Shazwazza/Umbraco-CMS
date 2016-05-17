using System.Threading;
using LightInject;
using Umbraco.Core.IO;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence.UnitOfWork;

namespace Umbraco.Core.Persistence.Repositories
{
    internal class PartialViewMacroRepository : PartialViewRepository, IPartialViewMacroRepository
    {
        
        public PartialViewMacroRepository(IUnitOfWork work, [Inject("PartialViewMacroFileSystem")] IFileSystem fileSystem, IOHelper ioHelper)
            : base(work, fileSystem, ioHelper)
        { }

        protected override PartialViewType ViewType => PartialViewType.PartialViewMacro;
    }
}