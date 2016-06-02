using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using AutoMapper;
using LightInject;
using Microsoft.AspNetCore.Hosting;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Configuration.UmbracoSettings;
using Umbraco.Core.DependencyInjection;
using Umbraco.Core.Exceptions;
using Umbraco.Core.IO;
using Umbraco.Core.Logging;
using Umbraco.Core.Manifest;
using Umbraco.Core.Models.Mapping;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.ObjectResolution;
using Umbraco.Core.Persistence.Mappers;
using Umbraco.Core.Persistence.Migrations;
using Umbraco.Core.Plugins;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.Services;
using Umbraco.Core.Sync;
using Umbraco.Core.Strings;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

//using Umbraco.Core._Legacy.PackageActions;


namespace Umbraco.Core
{

    /// <summary>
    /// A bootstrapper for the Umbraco application which initializes all objects for the Core of the application
    /// </summary>
    /// <remarks>
    /// This does not provide any startup functionality relating to web objects
    /// </remarks>
    public class CoreBootManager : IBootManager
    {
        protected ProfilingLogger ProfilingLogger { get; private set; }
        private DisposableTimer _timer;
        protected MapperConfiguration MapperConfiguration { get; private set; }

        private IServiceContainer _appStartupEvtContainer;
        private bool _isInitialized = false;
        private bool _isStarted = false;
        private bool _isComplete = false;
        private readonly UmbracoApplication _umbracoApplication;
        protected ApplicationContext ApplicationContext { get; private set; }        

        protected UmbracoApplication UmbracoApplication
        {
            get { return _umbracoApplication; }
        }

        public IServiceContainer Container
        {
            get { return _umbracoApplication.Container; }
        }
        
        public CoreBootManager(UmbracoApplication umbracoApplication)
        {
            if (umbracoApplication == null) throw new ArgumentNullException("umbracoApplication");
            _umbracoApplication = umbracoApplication;
        }
     
        public virtual IBootManager Initialize()
        {
            if (_isInitialized)
                throw new InvalidOperationException("The boot manager has already been initialized");
            
            ProfilingLogger = new ProfilingLogger(_umbracoApplication.Logger, _umbracoApplication.Profiler);
            
            _timer = ProfilingLogger.TraceDuration<CoreBootManager>(
                $"Umbraco {UmbracoVersion.GetSemanticVersion().ToSemanticString()} application starting on {_umbracoApplication.HostingEnvironment.EnvironmentName}",
                "Umbraco application startup complete");

            //set the singleton resolved from the core container
            ApplicationContext.Current = ApplicationContext = Container.GetInstance<ApplicationContext>();

            //TODO: Remove these for v8!
            LegacyPropertyEditorIdToAliasConverter.CreateMappingsForCoreEditors();
            LegacyParameterEditorAliasConverter.CreateMappingsForCoreEditors();

            //Create a 'child'container which is a copy of all of the current registrations and begin a sub scope for it
            // this child container will be used to manage the application event handler instances and the scope will be
            // completed at the end of the boot process to allow garbage collection
            var pluginMgr = Container.GetInstance<PluginManager>();
            _appStartupEvtContainer = Container.Clone();
            _appStartupEvtContainer.BeginScope();
            _appStartupEvtContainer.RegisterCollection<PerScopeLifetime>(pluginMgr.ResolveApplicationStartupHandlers());            
            
            InitializeResolvers();
            InitializeModelMappers();

            //now we need to call the initialize methods
            Parallel.ForEach(_appStartupEvtContainer.GetAllInstances<IApplicationEventHandler>(), x =>
            {
                try
                {
                    using (ProfilingLogger.DebugDuration<CoreBootManager>(
                        $"Executing {x.GetType()} in ApplicationInitialized",
                        $"Executed {x.GetType()} in ApplicationInitialized",
                        //only log if more than 150ms
                        150))
                    {
                        x.OnApplicationInitialized(UmbracoApplication, ApplicationContext);
                    }
                }
                catch (Exception ex)
                {
                    ProfilingLogger.Logger.Error<CoreBootManager>("An error occurred running OnApplicationInitialized for handler " + x.GetType(), ex);
                    throw;
                }
            });

            _isInitialized = true;

            return this;
        }       

        /// <summary>
        /// This method initializes all of the model mappers registered in the container
        /// </summary>        
        protected void InitializeModelMappers()
        {
            MapperConfiguration = new MapperConfiguration(configuration =>
            {
                foreach (var m in Container.GetAllInstances<ModelMapperConfiguration>())
                {
                    m.ConfigureMappings(configuration);
                }
            });
        }

        /// <summary>
        /// Fires after initialization and calls the callback to allow for customizations to occur &
        /// Ensure that the OnApplicationStarting methods of the IApplicationEvents are called
        /// </summary>
        /// <param name="afterStartup"></param>
        /// <returns></returns>
        public virtual IBootManager Startup(Action<ApplicationContext> afterStartup)
        {
            if (_isStarted)
                throw new InvalidOperationException("The boot manager has already been initialized");

            //call OnApplicationStarting of each application events handler
            Parallel.ForEach(_appStartupEvtContainer.GetAllInstances<IApplicationEventHandler>(), x =>
            {
                try
                {
                    using (ProfilingLogger.DebugDuration<CoreBootManager>(
                        $"Executing {x.GetType()} in ApplicationStarting",
                        $"Executed {x.GetType()} in ApplicationStarting",
                        //only log if more than 150ms
                        150))
                    {
                        x.OnApplicationStarting(UmbracoApplication, ApplicationContext);
                    }
                }
                catch (Exception ex)
                {
                    ProfilingLogger.Logger.Error<CoreBootManager>("An error occurred running OnApplicationStarting for handler " + x.GetType(), ex);
                    throw;
                }
            });

            if (afterStartup != null)
            {
                afterStartup(ApplicationContext.Current);
            }

            _isStarted = true;

            return this;
        }

        /// <summary>
        /// Fires after startup and calls the callback once customizations are locked
        /// </summary>
        /// <param name="afterComplete"></param>
        /// <returns></returns>
        public virtual IBootManager Complete(Action<ApplicationContext> afterComplete)
        {
            if (_isComplete)
                throw new InvalidOperationException("The boot manager has already been completed");

            FreezeResolution();

            //Here we need to make sure the db can be connected to
		    EnsureDatabaseConnection();


            //This is a special case for the user service, we need to tell it if it's an upgrade, if so we need to ensure that
            // exceptions are bubbled up if a user is attempted to be persisted during an upgrade (i.e. when they auth to login)
            ((UserService) ApplicationContext.Services.UserService).IsUpgrading = true;



            //call OnApplicationStarting of each application events handler
            Parallel.ForEach(_appStartupEvtContainer.GetAllInstances<IApplicationEventHandler>(), x =>
            {
                try
                {
                    using (ProfilingLogger.DebugDuration<CoreBootManager>(
                        $"Executing {x.GetType()} in ApplicationStarted",
                        $"Executed {x.GetType()} in ApplicationStarted", 
                        //only log if more than 150ms
                        150))
                    {
                        x.OnApplicationStarted(UmbracoApplication, ApplicationContext);
                    }
                }
                catch (Exception ex)
                {
                    ProfilingLogger.Logger.Error<CoreBootManager>("An error occurred running OnApplicationStarted for handler " + x.GetType(), ex);
                    throw;
                }
            });

            //end the current scope which was created to intantiate all of the startup handlers,
            //this will dispose them if they're IDisposable
            _appStartupEvtContainer.EndCurrentScope();
            //NOTE: DO NOT Dispose this cloned container since it will also dispose of any instances 
            // resolved from the parent container    
            _appStartupEvtContainer = null;

            if (afterComplete != null)
            {
                afterComplete(ApplicationContext.Current);
            }

            _isComplete = true;

            // we're ready to serve content!
            ApplicationContext.IsReady = true;

            //stop the timer and log the output
            _timer.Dispose();
            return this;
		}

        /// <summary>
        /// We cannot continue if the db cannot be connected to
        /// </summary>
        private void EnsureDatabaseConnection()
        {
            if (ApplicationContext.IsConfigured == false) return;
            if (ApplicationContext.DatabaseContext.IsDatabaseConfigured == false) return;

            //try now
            if (ApplicationContext.DatabaseContext.CanConnect)
                return;

            var currentTry = 0;
            while (currentTry < 5)
            {
                //first wait, then retry
                Thread.Sleep(1000);

                if (ApplicationContext.DatabaseContext.CanConnect)
                    break;

                currentTry++;
            }

            if (currentTry == 5)
            {
                throw new UmbracoStartupFailedException("Umbraco cannot start. A connection string is configured but the Umbraco cannot connect to the database.");
            }
        }

        /// <summary>
        /// Freeze resolution to not allow Resolvers to be modified
        /// </summary>
        protected virtual void FreezeResolution()
        {
            Resolution.Freeze();
        }

        /// <summary>
        /// Create the resolvers
        /// </summary>
        protected virtual void InitializeResolvers()
        {
            var pluginManager = Container.GetInstance<PluginManager>();

            PropertyEditorResolver.Current = new PropertyEditorResolver(
                Container, ProfilingLogger.Logger, () => pluginManager.ResolvePropertyEditors());
            ParameterEditorResolver.Current = new ParameterEditorResolver(
                Container, ProfilingLogger.Logger, () => pluginManager.ResolveParameterEditors());

            //setup the validators resolver with our predefined validators
            ValidatorsResolver.Current = new ValidatorsResolver(
                Container, ProfilingLogger.Logger, () => new[]
                {
                    typeof(RequiredManifestValueValidator),
                    typeof(RegexValidator),
                    typeof(DelimitedManifestValueValidator),
                    typeof(EmailValidator),
                    typeof(IntegerValidator),
                    typeof(DecimalValidator),
                });

            ////by default we'll use the db server registrar unless the developer has the legacy
            //// dist calls enabled, in which case we'll use the config server registrar
            //if (UmbracoConfig.For.UmbracoSettings().DistributedCall.Enabled)
            //{
            //    ServerRegistrarResolver.Current = new ServerRegistrarResolver(new ConfigServerRegistrar(UmbracoConfig.For.UmbracoSettings()));
            //}
            //else
            //{
            //    ServerRegistrarResolver.Current = new ServerRegistrarResolver(
            //        new DatabaseServerRegistrar(
            //            new Lazy<IServerRegistrationService>(() => ApplicationContext.Services.ServerRegistrationService),
            //            new DatabaseServerRegistrarOptions()));
            //}


            ////by default we'll use the database server messenger with default options (no callbacks),
            //// this will be overridden in the web startup
            //ServerMessengerResolver.Current = new ServerMessengerResolver(
            //    Container,
            //    factory => new DatabaseServerMessenger(ApplicationContext, true, new DatabaseServerMessengerOptions()));

            MappingResolver.Current = new MappingResolver(
                Container, ProfilingLogger.Logger,
                () => pluginManager.ResolveAssignedMapperTypes());


            //RepositoryResolver.Current = new RepositoryResolver(
            //    new RepositoryFactory(ApplicationCache));

            CacheRefreshersResolver.Current = new CacheRefreshersResolver(
                Container, ProfilingLogger.Logger,
                () => pluginManager.ResolveCacheRefreshers());
                        
            //PackageActionsResolver.Current = new PackageActionsResolver(
            //    ServiceProvider, ProfilingLogger.Logger,
            //    () => pluginManager.ResolvePackageActions());
            
            //the database migration objects
            MigrationResolver.Current = new MigrationResolver(
                Container, ProfilingLogger.Logger,
                () => pluginManager.ResolveTypes<IMigration>());


            // need to filter out the ones we dont want!!
            PropertyValueConvertersResolver.Current = new PropertyValueConvertersResolver(
                Container, ProfilingLogger.Logger,
                pluginManager.ResolveTypes<IPropertyValueConverter>());

            //// use the new DefaultShortStringHelper
            //ShortStringHelperResolver.Current = new ShortStringHelperResolver(Container,
            //    factory => new DefaultShortStringHelper(factory.GetInstance<IUmbracoSettingsSection>()).WithDefaultConfig());

            UrlSegmentProviderResolver.Current = new UrlSegmentProviderResolver(
                Container, ProfilingLogger.Logger,
                typeof(DefaultUrlSegmentProvider));

            // by default, no factory is activated
            PublishedContentModelFactoryResolver.Current = new PublishedContentModelFactoryResolver(Container);
        }
        
    }
}
