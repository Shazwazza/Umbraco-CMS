using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using LightInject;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.ObjectResolution;
using Umbraco.Core.Logging;

namespace Umbraco.Core
{
    /// <summary>
    /// The Umbraco application class that manages bootup and shutdown sequences
    /// </summary>
    public class UmbracoApplication : DisposableObject
    {
        public IHostingEnvironment HostingEnvironment { get; set; }
        private readonly IBootManager _bootManager;

        /// <summary>
        /// Umbraco application's IoC container
        /// </summary>
        public IServiceContainer Container { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        public UmbracoApplication(IHostingEnvironment hostingEnvironment, IApplicationLifetime applicationLifetime, IConfiguration umbracoConfig)
        {
            HostingEnvironment = hostingEnvironment;
            //TODO: This must be initialized FIRST
            Logger = null;
            Profiler = null;

            // create the container for the application, the boot managers are responsible for registrations
            var container = new ServiceContainer();
            container.EnableAnnotatedConstructorInjection();

            Container = container;

            //register the application shutdown handler
            applicationLifetime.ApplicationStopping.Register(DisposeResources);

            _bootManager = new CoreBootManager(this, umbracoConfig);           
        }

        public event EventHandler ApplicationStarting;
        public event EventHandler ApplicationStarted;
        public event EventHandler ApplicationError;
        public event EventHandler ApplicationEnd;


        /// <summary>
        /// Boots up the Umbraco application
        /// </summary>
        public void StartApplication()
        {
            Thread.CurrentThread.SanitizeThreadCulture();

#if NET461
            //take care of unhandled exceptions - there is nothing we can do to 
            // prevent the entire w3wp process to go down but at least we can try
            // and log the exception
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                var exception = (Exception)args.ExceptionObject;
                var isTerminating = args.IsTerminating; // always true?

                var msg = "Unhandled exception in AppDomain";
                if (isTerminating) msg += " (terminating)";
                Logger.Error<UmbracoApplication>(msg, exception);
            }; 
#endif

            //boot up the application
            GetBootManager()
                .Initialize()
                .Startup(appContext => OnApplicationStarting(new EventArgs()))
                .Complete(appContext => OnApplicationStarted(new EventArgs()));
        }

        /// <summary>
        /// Developers can override this method to modify objects on startup
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnApplicationStarting(EventArgs e)
        {
            if (ApplicationStarting != null)
            {
                try
                {
                    ApplicationStarting(this, e);
                }
                catch (Exception ex)
                {
                    LogHelper.Error<UmbracoApplication>("An error occurred in an ApplicationStarting event handler", ex);
                    throw;
                }
            }

        }

        /// <summary>
        /// Developers can override this method to do anything they need to do once the application startup routine is completed.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnApplicationStarted(EventArgs e)
        {
            if (ApplicationStarted != null)
            {
                try
                {
                    ApplicationStarted(this, e);
                }
                catch (Exception ex)
                {
                    LogHelper.Error<UmbracoApplication>("An error occurred in an ApplicationStarted event handler", ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// A method that can be overridden to invoke code when the application has an error.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnApplicationError(object sender, EventArgs e)
        {
            var handler = ApplicationError;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            //TODO: This might be done with something called DiagnosticListener ?

            // Code that runs when an unhandled error occurs

            //// Get the exception object.
            //var exc = Server.GetLastError();
            //// Ignore HTTP errors
            //if (exc.GetType() == typeof(HttpException))
            //{
            //    return;
            //}

            //Logger.Error<UmbracoApplicationBase>("An unhandled exception occurred", exc);

            OnApplicationError(sender, e);
        }

        /// <summary>
        /// A method that can be overridden to invoke code when the application shuts down.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnApplicationEnd(EventArgs e)
        {
            var handler = ApplicationEnd;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        protected override void DisposeResources()
        {
            var msg = "Application shutdown. Details? ";
            Logger.Info<UmbracoApplication>(msg);
            OnApplicationEnd(new EventArgs());
        }

        protected IBootManager GetBootManager()
        {
            return _bootManager;
        }

        /// <summary>
        /// Returns the logger instance for the application - this will be used throughout the entire app
        /// </summary>
        public virtual ILogger Logger { get; }

        /// <summary>
        /// Returns the Profiler instance for the application - this will be used throughout the entire app
        /// </summary>
        public virtual IProfiler Profiler { get; }
    }
}
