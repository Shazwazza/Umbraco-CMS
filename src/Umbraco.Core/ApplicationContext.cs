using System;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.ObjectResolution;
using Umbraco.Core.Services;
using Umbraco.Core.Sync;

namespace Umbraco.Core
{
	/// <summary>
    /// Represents the Umbraco application context.
    /// </summary>
    /// <remarks>Only one singleton instance per running Umbraco application (AppDomain)</remarks>
    public class ApplicationContext : IDisposable
    {
        private volatile bool _disposed;
        private readonly ReaderWriterLockSlim _disposalLocker = new ReaderWriterLockSlim();
        private bool _isReady;
        readonly ManualResetEventSlim _isReadyEvent = new ManualResetEventSlim(false);
        private DatabaseContext _databaseContext;
        private ServiceContext _services;
	    private readonly IUmbracoConfig _umbracoConfig;
	    private Lazy<bool> _configured;

        // ReSharper disable once InconsistentNaming
        internal string _umbracoApplicationUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationContext"/> class.
        /// </summary>
        /// <param name="dbContext">A database context.</param>
        /// <param name="serviceContext">A service context.</param>
        /// <param name="cache">A cache helper.</param>
        /// <param name="logger">A logger.</param>
        /// <param name="umbracoConfig"></param>
        public ApplicationContext(DatabaseContext dbContext, ServiceContext serviceContext, CacheHelper cache, ProfilingLogger logger, IUmbracoConfig umbracoConfig)
	    {
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));
            if (serviceContext == null) throw new ArgumentNullException(nameof(serviceContext));
            if (cache == null) throw new ArgumentNullException(nameof(cache));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            _databaseContext = dbContext;
            _services = serviceContext;
	        _umbracoConfig = umbracoConfig;

	        ApplicationCache = cache;
            ProfilingLogger = logger;

            Initialize();
	    }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationContext"/> class.
        /// </summary>
        /// <param name="cache">A cache helper.</param>
        /// <param name="logger">A logger.</param>
        /// <param name="umbracoConfig"></param>
        /// <remarks>For Unit Tests only.</remarks>
        public ApplicationContext(CacheHelper cache, ProfilingLogger logger, IUmbracoConfig umbracoConfig)
        {
	        if (cache == null) throw new ArgumentNullException(nameof(cache));
	        if (logger == null) throw new ArgumentNullException(nameof(logger));

            _umbracoConfig = umbracoConfig;

            ApplicationCache = cache;
	        ProfilingLogger = logger;

            Initialize();
        }

	    /// <summary>
	    /// Sets and/or ensures that a global application context exists.
	    /// </summary>
	    /// <param name="appContext">The application context instance.</param>
	    /// <param name="replaceContext">A value indicating whether to replace the existing context, if any.</param>
	    /// <returns>The current global application context.</returns>
	    /// <remarks>This is NOT thread safe. For Unit Tests only.</remarks>
	    public static ApplicationContext EnsureContext(ApplicationContext appContext, bool replaceContext)
	    {
            if (Current != null && replaceContext == false)
                return Current;

	        return Current = appContext;
	    }

	    /// <summary>
	    /// Sets and/or ensures that a global application context exists.
	    /// </summary>
	    /// <param name="cache">A cache helper.</param>
	    /// <param name="logger">A logger.</param>
	    /// <param name="dbContext">A database context.</param>
	    /// <param name="serviceContext">A service context.</param>
	    /// <param name="umbracoConfiggs"></param>
	    /// <param name="replaceContext">A value indicating whether to replace the existing context, if any.</param>
	    /// <returns>The current global application context.</returns>
	    /// <remarks>This is NOT thread safe. For Unit Tests only.</remarks>
	    public static ApplicationContext EnsureContext(DatabaseContext dbContext, ServiceContext serviceContext, CacheHelper cache, ProfilingLogger logger, IUmbracoConfig umbracoConfig, bool replaceContext)
        {
            if (Current != null && replaceContext == false)
                    return Current;

            return Current = new ApplicationContext(dbContext, serviceContext, cache, logger, umbracoConfig);
        }

	    /// <summary>
    	/// Gets the current global application context.
    	/// </summary>
    	public static ApplicationContext Current { get; internal set; }

		/// <summary>
		/// Returns the application wide cache accessor
		/// </summary>
		/// <remarks>
		/// Any caching that is done in the application (app wide) should be done through this property
		/// </remarks>
		public CacheHelper ApplicationCache { get; private set; }

        /// <summary>
        /// Gets the profiling logger.
        /// </summary>
        public ProfilingLogger ProfilingLogger { get; }

        /// <summary>
        /// Gets a value indicating whether the application is configured.
        /// </summary>
        /// <remarks>Meaning: installed and no need to upgrade anything.</remarks>
        public virtual bool IsConfigured => _configured.Value;

        /// <summary>
        /// Gets a value indicating whether the application is ready.
        /// </summary>
        /// <remarks><para>Meaning: ready to run, boot has completed, though maybe the application is not configured.</para>
        /// <para>IsReady is set to true by the boot manager once it has successfully booted. The original
        /// Umbraco module checked on content.Instance, now the boot task that sets the content
        /// store ensures that it is ready.</para>
        /// </remarks>
        public bool IsReady
        {
            get
            {
                return _isReady;
            }
            internal set
            {
                if (IsReady)
                    throw new Exception("ApplicationContext has already been initialized.");
                if (value == false)
                    throw new Exception("Value must be true.");

                _isReady = true;
				_isReadyEvent.Set();
            }
        }

        /// <summary>
        /// Blocks until the application is ready.
        /// </summary>
        /// <param name="timeout">The time to wait, or -1 to wait indefinitely.</param>
        /// <returns>A value indicating whether the application is ready.</returns>
		public bool WaitForReady(int timeout)
		{
			return _isReadyEvent.WaitHandle.WaitOne(timeout);
		}

	    /// <summary>
	    /// Gets a value indicating whether the application is upgrading.
        /// </summary>
        /// <remarks>Meaning: the database is configured and the database context has access to an existing Umbraco schema,
        /// however we are not 'configured' because we still need to upgrade.</remarks>
	    public bool IsUpgrading
	    {
            get
            {
                if (IsConfigured // configured already
                    || DatabaseContext == null // no database context
                    || DatabaseContext.IsDatabaseConfigured == false // database is not configured
                    || DatabaseContext.CanConnect == false) // database cannot connect
                    return false;

                // upgrading if we have some valid tables (else, no schema, need to install)
                var schemaresult = DatabaseContext.ValidateDatabaseSchema();
                return schemaresult.ValidTables.Count > 0;
            }
	    }        

	    private void Initialize()
		{
            ResetConfigured();
		}

        /// <summary>
        /// Resets the IsConfigured value, which will then be discovered again.
        /// </summary>
        /// <remarks>For Unit Tests usage, though it is harmless.</remarks>
	    public void ResetConfigured()
	    {
            // create the lazy value to resolve whether or not the application is 'configured'
            // meaning: installed and no need to upgrade anything
            _configured = new Lazy<bool>(() =>
            {
                try
                {
                    var configStatus = ConfigurationStatus; // the value in the web.config
                    var currentVersion = UmbracoVersion.GetSemanticVersion(); // the hard-coded current version of the binaries that are executing

                    // if we have no value in web.config or value differs, we are not configured yet
                    if (string.IsNullOrWhiteSpace(configStatus) || configStatus != currentVersion)
                    {
                        ProfilingLogger.Logger.Debug<ApplicationContext>($"CurrentVersion different from configStatus: '{currentVersion.ToSemanticString()}','{configStatus}'.");
                        return false;
                    }

                    // versions match, now look for database state and migrations
                    // which requires that we do have a database that we can connect to
                    if (DatabaseContext.IsDatabaseConfigured == false || DatabaseContext.CanConnect == false)
                    {
                        ProfilingLogger.Logger.Debug<ApplicationContext>("Database is not configured, or could not connect to the database.");
                        return false;
                    }

                    // look for a migration entry for the current version
                    var entry = Services.MigrationEntryService.FindEntry(Constants.System.UmbracoMigrationName, UmbracoVersion.GetSemanticVersion());
                    if (entry != null)
                        return true; // all clear!

                    // even though the versions match, the db has not been properly upgraded
                    ProfilingLogger.Logger.Debug<ApplicationContext>($"The migration for version: '{currentVersion.ToSemanticString()} has not been executed, there is no record in the database.");
                    return false;
                }
                catch (Exception ex)
                {
                    LogHelper.Error<ApplicationContext>("Error determining if application is configured, returning false.", ex);
                    return false;
                }
            });
        }

        // gets the configuration status, ie the version that's in web.config
        private string ConfigurationStatus => _umbracoConfig.ConfigurationStatus;

	    /// <summary>
		/// Gets the current database context.
		/// </summary>
		public DatabaseContext DatabaseContext
		{
			get
			{
				if (_databaseContext == null)
					throw new InvalidOperationException("The DatabaseContext has not been set on the ApplicationContext.");
				return _databaseContext;
			}
            // INTERNAL FOR UNIT TESTS
			internal set { _databaseContext = value; }
		}

		/// <summary>
		/// Gets the current service context.
		/// </summary>
		public ServiceContext Services
		{
			get
			{
				if (_services == null)
					throw new InvalidOperationException("The ServiceContext has not been set on the ApplicationContext.");
				return _services;
			}
            // INTERNAL FOR UNIT TESTS
            internal set { _services = value; }
		}

        /// <summary>
        /// Disposes the application context.
        /// </summary>
        /// <remarks>Do not this object if you require the Umbraco application to run. Disposing this object
        /// is generally used for unit testing and when your application is shutting down after you have booted Umbraco.
        /// </remarks>
        void IDisposable.Dispose()
        {
            if (_disposed) return;

            using (new WriteLock(_disposalLocker))
            {
                // double check... bah...
                if (_disposed) return;

                // clear the cache
                if (ApplicationCache != null)
                {
                    ApplicationCache.RuntimeCache.ClearAllCache();
                    ApplicationCache.IsolatedRuntimeCache.ClearAllCaches();
                }

                // reset all resolvers
                ResolverCollection.ResetAll();

                // reset resolution itself (though this should be taken care of by resetting any of the resolvers above)
                Resolution.Reset();

                // reset the instance objects
                ApplicationCache = null;
                if (_databaseContext != null) //need to check the internal field here
                {
                    if (DatabaseContext.IsDatabaseConfigured)
                        DatabaseContext.Database?.Dispose();
                }

                DatabaseContext = null;
                Services = null;
                _isReady = false; //set the internal field

                _disposed = true;
            }
        }
    }
}
