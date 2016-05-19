using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using LightInject;
using Microsoft.AspNet.Hosting;
using Umbraco.Core.Logging;
using Umbraco.Core.ObjectResolution;
using Umbraco.Core.Logging;

namespace Umbraco.Core
{

    /// <summary>
    /// The Umbraco application class that manages bootup and shutdown sequences
    /// </summary>
    public abstract class UmbracoApplicationBase : DisposableObject
    {
        private readonly IBootManager _bootManager;

        /// <summary>
        /// Umbraco application's IoC container
        /// </summary>
        public IServiceContainer Container { get; }

        private ILogger _logger;

        /// <summary>
        /// Constructor
        /// </summary>
        protected UmbracoApplicationBase(IBootManager bootManager, IApplicationLifetime applicationLifetime)
        {
            _bootManager = bootManager;

            // create the container for the application, the boot managers are responsible for registrations
             var container = new ServiceContainer();
            container.EnableAnnotatedConstructorInjection();

            Container = container;

            //register the application shutdown handler
            applicationLifetime.ApplicationStopping.Register(DisposeResources);
        }

        public event EventHandler ApplicationStarting;
        public event EventHandler ApplicationStarted;
        public event EventHandler ApplicationError;
        public event EventHandler ApplicationEnd;


        /// <summary>
        /// Boots up the Umbraco application
        /// </summary>
        internal void StartApplication(object sender, EventArgs e)
        {
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
                Logger.Error<UmbracoApplicationBase>(msg, exception);
            }; 
#endif

            //boot up the application
            GetBootManager()
                .Initialize()
                .Startup(appContext => OnApplicationStarting(sender, e))
                .Complete(appContext => OnApplicationStarted(sender, e));
        }

        /// <summary>
        /// Initializes the Umbraco application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Application_Start(object sender, EventArgs e)
        {
            Thread.CurrentThread.SanitizeThreadCulture();
            StartApplication(sender, e);
        }

        /// <summary>
        /// Developers can override this method to modify objects on startup
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnApplicationStarting(object sender, EventArgs e)
        {
            if (ApplicationStarting != null)
            {
                try
                {
                    ApplicationStarting(sender, e);
                }
                catch (Exception ex)
                {
                    LogHelper.Error<UmbracoApplicationBase>("An error occurred in an ApplicationStarting event handler", ex);
                    throw;
                }
            }
                
        }

        /// <summary>
        /// Developers can override this method to do anything they need to do once the application startup routine is completed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnApplicationStarted(object sender, EventArgs e)
        {
            if (ApplicationStarted != null)
            {
                try
                {
                    ApplicationStarted(sender, e);
                }
                catch (Exception ex)
                {
                    LogHelper.Error<UmbracoApplicationBase>("An error occurred in an ApplicationStarted event handler", ex);
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
            Logger.Info<UmbracoApplicationBase>(msg);
            OnApplicationEnd(new EventArgs());            
        }

        protected IBootManager GetBootManager()
        {
            return _bootManager;
        }

        /// <summary>
        /// Returns the logger instance for the application - this will be used throughout the entire app
        /// </summary>
        public virtual ILogger Logger => _logger ?? (_logger = Container.GetInstance<ILogger>());
    }
}
