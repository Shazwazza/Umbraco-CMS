using System;
using System.Collections.Generic;
using System.ComponentModel;
using Umbraco.Core.Configuration;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence.Migrations;

namespace Umbraco.Core.Events
{
    public class MigrationEventArgs : CancellableObjectEventArgs<IList<IMigration>>
    {
        /// <summary>
        /// Constructor accepting multiple migrations that are used in the migration runner
        /// </summary>
        /// <param name="eventObject"></param>
        /// <param name="targetVersion"></param>
        /// <param name="productName"></param>
        /// <param name="canCancel"></param>
        /// <param name="configuredVersion"></param>
        public MigrationEventArgs(IList<IMigration> eventObject, SemVersion configuredVersion, SemVersion targetVersion, string productName, bool canCancel)
            : this(eventObject, null, configuredVersion, targetVersion, productName, canCancel)
        { }

        /// <summary>
        /// Constructor accepting multiple migrations that are used in the migration runner
        /// </summary>
        /// <param name="eventObject"></param>
        /// <param name="migrationContext"></param>
        /// <param name="targetVersion"></param>
        /// <param name="productName"></param>
        /// <param name="canCancel"></param>
        /// <param name="configuredVersion"></param>
        internal MigrationEventArgs(IList<IMigration> eventObject, IMigrationContext migrationContext, SemVersion configuredVersion, SemVersion targetVersion, string productName, bool canCancel)
            : base(eventObject, canCancel)
        {
            MigrationContext = migrationContext;
            ConfiguredSemVersion = configuredVersion;
            TargetSemVersion = targetVersion;
            ProductName = productName;
        }

        /// <summary>
        /// Constructor accepting multiple migrations that are used in the migration runner
        /// </summary>
        /// <param name="eventObject"></param>
        /// <param name="configuredVersion"></param>
        /// <param name="targetVersion"></param>
        /// <param name="productName"></param>
        public MigrationEventArgs(IList<IMigration> eventObject, SemVersion configuredVersion, SemVersion targetVersion, string productName)
            : this(eventObject, null, configuredVersion, targetVersion, productName, false)
        { }

        /// <summary>
        /// Returns all migrations that were used in the migration runner
        /// </summary>
        public IList<IMigration> Migrations
        {
            get { return EventObject; }
        }

        public SemVersion ConfiguredSemVersion { get; private set; }

        public SemVersion TargetSemVersion { get; private set; }

        public string ProductName { get; private set; }

        public IMigrationContext MigrationContext { get; private set; }
    }
}