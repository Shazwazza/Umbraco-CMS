
using System;

namespace Umbraco.Core.Configuration.UmbracoSettings
{

    public class UmbracoSettingsSection : IUmbracoSettingsSection
    {
        public UmbracoSettingsSection()
        {
            Content = new ContentElement();
        }

        //bindable
        internal ContentElement Content { get; set; }

        //explicit
        IContentSection IUmbracoSettingsSection.Content
        {
            get { return Content; }
        }

        //public IDeveloperSection Developer { get; set; }

        //public IDistributedCallSection DistributedCall { get; set; }

        //public ILoggingSection Logging { get; set; }

        //public IRepositoriesSection PackageRepositories { get; set; }

        //public IProvidersSection Providers { get; set; }

        //public IRequestHandlerSection RequestHandler { get; set; }

        //public IScheduledTasksSection ScheduledTasks { get; set; }

        //public ISecuritySection Security { get; set; }

        //public IWebRoutingSection WebRouting { get; set; }

        //[ConfigurationProperty("repositories")]
        //internal RepositoriesElement PackageRepositories
        //{
        //    get
        //    {

        //        if (_defaultRepositories != null)
        //        {
        //            return _defaultRepositories;
        //        }

        //        //here we need to check if this element is defined, if it is not then we'll setup the defaults
        //        var prop = Properties["repositories"];
        //        var repos = this[prop] as ConfigurationElement;
        //        if (repos != null && repos.ElementInformation.IsPresent == false)
        //        {
        //            var collection = new RepositoriesCollection
        //                {
        //                    new RepositoryElement() {Name = "Umbraco package Repository", Id = new Guid("65194810-1f85-11dd-bd0b-0800200c9a66")}
        //                };


        //            _defaultRepositories = new RepositoriesElement()
        //                {
        //                    Repositories = collection
        //                };

        //            return _defaultRepositories;
        //        }

        //        //now we need to ensure there is *always* our umbraco repo! its hard coded in the codebase!
        //        var reposElement = (RepositoriesElement)base["repositories"];
        //        if (reposElement.Repositories.All(x => x.Id != new Guid("65194810-1f85-11dd-bd0b-0800200c9a66")))
        //        {
        //            reposElement.Repositories.Add(new RepositoryElement() { Name = "Umbraco package Repository", Id = new Guid("65194810-1f85-11dd-bd0b-0800200c9a66") });                    
        //        }

        //        return reposElement;
        //    }
        //}

    }
}
