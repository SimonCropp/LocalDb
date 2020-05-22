using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
// ReSharper disable RedundantCast

namespace EfLocalDb
{
    public class SqlInstance<TDbContext>
        where TDbContext : DbContext
    {
        internal Wrapper Wrapper { get; private set; } = null!;
        ConstructInstance<TDbContext> constructInstance = null!;
        static Storage DefaultStorage;

        static SqlInstance()
        {
            var instanceName = typeof(TDbContext).Name;
            DefaultStorage = new Storage(instanceName, DirectoryFinder.Find(instanceName));
        }

        public string ServerName => Wrapper.ServerName;

        public SqlInstance(
            ConstructInstance<TDbContext> constructInstance,
            TemplateFromContext<TDbContext>? buildTemplate = null,
            Storage? storage = null,
            DateTime? timestamp = null,
            ushort templateSize = 3,
            string? templatePath = null,
            string? logPath = null):
            this(
                constructInstance,
                BuildTemplateConverter.Convert(constructInstance, buildTemplate),
                storage,
                GetTimestamp(timestamp, buildTemplate),
                templateSize,
                templatePath,
                logPath)
        {
        }

        public SqlInstance(
            ConstructInstance<TDbContext> constructInstance,
            TemplateFromConnection buildTemplate,
            Storage? storage = null,
            DateTime? timestamp = null,
            ushort templateSize = 3,
            string? templatePath = null,
            string? logPath = null)
        {
            storage ??= DefaultStorage;

            var resultTimestamp = GetTimestamp(timestamp, buildTemplate);
            Guard.AgainstNull(nameof(constructInstance), constructInstance);
            this.constructInstance = constructInstance;

            DirectoryCleaner.CleanInstance(storage.Value.Directory);
            Task BuildTemplate(DbConnection connection)
            {
                return buildTemplate(connection);
            }

            Wrapper = new Wrapper(s => new SqlConnection(s), storage.Value.Name, storage.Value.Directory, templateSize, templatePath, logPath);
            Wrapper.Start(resultTimestamp, BuildTemplate);
        }

        static DateTime GetTimestamp(DateTime? timestamp, Delegate? buildTemplate)
        {
            if (timestamp != null)
            {
                return timestamp.Value;
            }

            if (buildTemplate != null)
            {
                return Timestamp.LastModified(buildTemplate);
            }

            return Timestamp.LastModified<TDbContext>();
        }

        public void Cleanup() => Wrapper.DeleteInstance();

        Task<string> BuildDatabase(string dbName)
        {
            return Wrapper.CreateDatabaseFromTemplate(dbName);
        }

        /// <summary>
        ///   Build DB with a name based on the calling Method.
        /// </summary>
        /// <param name="data">The seed data.</param>
        /// <param name="testFile">The path to the test class. Used to make the db name unique per test type.</param>
        /// <param name="databaseSuffix">For Xunit theories add some text based on the inline data to make the db name unique.</param>
        /// <param name="memberName">Used to make the db name unique per method. Will default to the caller method name is used.</param>
        public Task<SqlDatabase<TDbContext>> Build(
            IEnumerable<object>? data,
            [CallerFilePath] string testFile = "",
            string? databaseSuffix = null,
            [CallerMemberName] string memberName = "")
        {
            Guard.AgainstNullWhiteSpace(nameof(testFile), testFile);
            Guard.AgainstNullWhiteSpace(nameof(memberName), memberName);
            Guard.AgainstWhiteSpace(nameof(databaseSuffix), databaseSuffix);

            var testClass = Path.GetFileNameWithoutExtension(testFile);

            var dbName = DbNamer.DeriveDbName(databaseSuffix, memberName, testClass);
            return Build(dbName, data);
        }

        /// <summary>
        ///   Build DB with a name based on the calling Method.
        /// </summary>
        /// <param name="testFile">The path to the test class. Used to make the db name unique per test type.</param>
        /// <param name="databaseSuffix">For Xunit theories add some text based on the inline data to make the db name unique.</param>
        /// <param name="memberName">Used to make the db name unique per method. Will default to the caller method name is used.</param>
        public Task<SqlDatabase<TDbContext>> Build(
            [CallerFilePath] string testFile = "",
            string? databaseSuffix = null,
            [CallerMemberName] string memberName = "")
        {
            return Build(null, testFile, databaseSuffix, memberName);
        }

        public async Task<SqlDatabase<TDbContext>> Build(
            string dbName,
            IEnumerable<object>? data)
        {
            Guard.AgainstNullWhiteSpace(nameof(dbName), dbName);
            var connection = await BuildDatabase(dbName);
            var database = new SqlDatabase<TDbContext>(
                connection,
                dbName,
                constructInstance,
                () => Wrapper.DeleteDatabase(dbName),
                data);
            await database.Start();
            return database;
        }

        public Task<SqlDatabase<TDbContext>> Build(string dbName)
        {
            return Build(dbName, (IEnumerable<object>?) null);
        }

        ///// <summary>
        /////   Build DB with a transaction that is rolled back when disposed.
        ///// </summary>
        ///// <param name="data">The seed data.</param>
        //public Task<SqlDatabaseWithRollback<TDbContext>> BuildWithRollback(params object[] data)
        //{
        //    return BuildWithRollback((IEnumerable<object>)data);
        //}

        ///// <summary>
        /////   Build DB with a transaction that is rolled back when disposed.
        ///// </summary>
        ///// <param name="data">The seed data.</param>
        //public async Task<SqlDatabaseWithRollback<TDbContext>> BuildWithRollback(IEnumerable<object> data)
        //{
        //    var connection = await BuildWithRollbackDatabase();
        //    var database = new SqlDatabaseWithRollback<TDbContext>(connection, constructInstance, data);
        //    await database.Start();
        //    return database;
        //}

        //async Task<string> BuildWithRollbackDatabase()
        //{
        //    await wrapper.CreateWithRollbackDatabase();
        //    return wrapper.WithRollbackConnectionString;
        //}

        public string MasterConnectionString => Wrapper.MasterConnectionString;
    }
}