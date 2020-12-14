using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
// ReSharper disable RedundantCast

namespace EfLocalDb
{
    public class SqlInstance<TDbContext>
        where TDbContext : DbContext
    {
        internal Wrapper Wrapper { get; }
        ConstructInstance<TDbContext> constructInstance;
        static Storage DefaultStorage;
        Action<SqlServerDbContextOptionsBuilder>? sqlOptionsBuilder;

        static SqlInstance()
        {
            var name = typeof(TDbContext).Name;
            DefaultStorage = new(name, DirectoryFinder.Find(name));
        }

        public IModel Model { get; }

        public string ServerName => Wrapper.ServerName;

        /// <summary>
        /// Instantiate a <see cref="SqlInstance{TDbContext}"/>.
        /// Should usually be scoped as once instance per appdomain. So all tests use the same instance of <see cref="SqlInstance{TDbContext}"/>.
        /// </summary>
        /// <param name="constructInstance"></param>
        /// <param name="buildTemplate"></param>
        /// <param name="storage">Disk storage convention for where the mdb and the ldf files will be located.</param>
        /// <param name="timestamp"></param>
        /// <param name="templateSize">The size in MB for the template. Optional.</param>
        /// <param name="existingTemplate">Existing mdb and the ldf files to use when building the template. Optional.</param>
        /// <param name="callback">Option callback that is executed after the template database has been created.</param>
        /// <param name="sqlOptionsBuilder">Passed to <see cref="SqlServerDbContextOptionsExtensions.UseSqlServer(DbContextOptionsBuilder,string,Action{SqlServerDbContextOptionsBuilder})"/>.</param>
        public SqlInstance(
            ConstructInstance<TDbContext> constructInstance,
            TemplateFromContext<TDbContext>? buildTemplate = null,
            Storage? storage = null,
            DateTime? timestamp = null,
            ushort templateSize = 3,
            ExistingTemplate? existingTemplate = null,
            Callback<TDbContext>? callback = null,
            Action<SqlServerDbContextOptionsBuilder>? sqlOptionsBuilder = null) :
            this(
                constructInstance,
                BuildTemplateConverter.Convert(constructInstance, buildTemplate),
                storage,
                GetTimestamp(timestamp, buildTemplate),
                templateSize,
                existingTemplate,
                callback,
                sqlOptionsBuilder)
        {
        }

        /// <summary>
        /// Instantiate a <see cref="SqlInstance{TDbContext}"/>.
        /// Should usually be scoped as once instance per appdomain. So all tests use the same instance of <see cref="SqlInstance{TDbContext}"/>.
        /// </summary>
        /// <param name="constructInstance"></param>
        /// <param name="buildTemplate"></param>
        /// <param name="storage">Disk storage convention for where the mdb and the ldf files will be located. Optional.</param>
        /// <param name="timestamp"></param>
        /// <param name="templateSize">The size in MB for the template. Optional.</param>
        /// <param name="existingTemplate">Existing mdb and the ldf files to use when building the template. Optional.</param>
        /// <param name="callback">Callback that is executed after the template database has been created. Optional.</param>
        /// <param name="sqlOptionsBuilder">Passed to <see cref="SqlServerDbContextOptionsExtensions.UseSqlServer(DbContextOptionsBuilder,string,Action{SqlServerDbContextOptionsBuilder})"/>.</param>
        public SqlInstance(
            ConstructInstance<TDbContext> constructInstance,
            TemplateFromConnection<TDbContext> buildTemplate,
            Storage? storage = null,
            DateTime? timestamp = null,
            ushort templateSize = 3,
            ExistingTemplate? existingTemplate = null,
            Callback<TDbContext>? callback = null,
            Action<SqlServerDbContextOptionsBuilder>? sqlOptionsBuilder = null)
        {
            storage ??= DefaultStorage;
            var resultTimestamp = GetTimestamp(timestamp, buildTemplate);
            Guard.AgainstNull(nameof(buildTemplate), buildTemplate);
            Guard.AgainstNull(nameof(constructInstance), constructInstance);
            Model = BuildModel(constructInstance);
            this.constructInstance = constructInstance;
            if (sqlOptionsBuilder == null)
            {
                this.sqlOptionsBuilder = builder => { builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery); };
            }
            else
            {
                this.sqlOptionsBuilder = builder =>
                {
                    builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    sqlOptionsBuilder(builder);
                };
            }

            var storageValue = storage.Value;
            DirectoryCleaner.CleanInstance(storageValue.Directory);

            Task BuildTemplate(DbConnection connection)
            {
                var builder = DefaultOptionsBuilder.Build<TDbContext>();
                builder.UseSqlServer(connection, sqlOptionsBuilder);
                return buildTemplate(connection, builder);
            }

            Func<DbConnection, Task>? wrapperCallback = null;
            if (callback != null)
            {
                wrapperCallback = async connection =>
                {
                    var builder = DefaultOptionsBuilder.Build<TDbContext>();
                    builder.UseSqlServer(connection, sqlOptionsBuilder);
#if NET5_0
                    await using var context = constructInstance(builder);
#else
                    using var context = constructInstance(builder);
#endif
                    await callback(connection, context);
                };
            }

            Wrapper = new(
                s => new SqlConnection(s),
                storageValue.Name,
                storageValue.Directory,
                templateSize,
                existingTemplate,
                wrapperCallback);

            Wrapper.Start(resultTimestamp, BuildTemplate);
        }

        static DateTime GetTimestamp(DateTime? timestamp, Delegate? buildTemplate)
        {
            if (timestamp != null)
            {
                return timestamp.Value;
            }

            if (buildTemplate == null)
            {
                return Timestamp.LastModified<TDbContext>();
            }

            return Timestamp.LastModified(buildTemplate);
        }

        static IModel BuildModel(ConstructInstance<TDbContext> constructInstance)
        {
            var builder = DefaultOptionsBuilder.Build<TDbContext>();
            builder.UseSqlServer("Fake");
            return constructInstance(builder).Model;
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
            SqlDatabase<TDbContext> database = new(
                connection,
                dbName,
                constructInstance,
                () => Wrapper.DeleteDatabase(dbName),
                data,
                sqlOptionsBuilder);
            await database.Start();
            return database;
        }

        public Task<SqlDatabase<TDbContext>> Build(string dbName)
        {
            return Build(dbName, (IEnumerable<object>?) null);
        }

        /// <summary>
        ///   Build DB with a transaction that is rolled back when disposed.
        /// </summary>
        /// <param name="data">The seed data.</param>
        public Task<SqlDatabaseWithRollback<TDbContext>> BuildWithRollback(params object[] data)
        {
            return BuildWithRollback((IEnumerable<object>) data);
        }

        /// <summary>
        ///   Build DB with a transaction that is rolled back when disposed.
        /// </summary>
        /// <param name="data">The seed data.</param>
        public async Task<SqlDatabaseWithRollback<TDbContext>> BuildWithRollback(IEnumerable<object> data)
        {
            var connection = await BuildWithRollbackDatabase();
            SqlDatabaseWithRollback<TDbContext> database = new(
                connection,
                constructInstance,
                data,
                sqlOptionsBuilder);
            await database.Start();
            return database;
        }

        async Task<string> BuildWithRollbackDatabase()
        {
            await Wrapper.CreateWithRollbackDatabase();
            return Wrapper.WithRollbackConnectionString;
        }

        public string MasterConnectionString => Wrapper.MasterConnectionString;
    }
}