using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EfLocalDb
{
    public class SqlInstance<TDbContext>
        where TDbContext : DbContext
    {
        Wrapper wrapper;
        Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance;

        public string ServerName => wrapper.ServerName;

        public SqlInstance(
            Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance,
            Action<TDbContext> buildTemplate = null,
            string instanceSuffix = null,
            Func<TDbContext, bool> requiresRebuild = null,
            ushort templateSize = 3)
        {
            Guard.AgainstWhiteSpace(nameof(instanceSuffix), instanceSuffix);
            Guard.AgainstNull(nameof(constructInstance), constructInstance);
            var instanceName = GetInstanceName(instanceSuffix);
            var directory = DirectoryFinder.Find(instanceName);

            Init(
                ConvertBuildTemplate(constructInstance, buildTemplate),
                constructInstance,
                instanceName,
                directory,
                requiresRebuild,
                templateSize);
        }

        public SqlInstance(
            Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance,
            string name,
            string directory,
            Action<TDbContext> buildTemplate = null,
            Func<TDbContext, bool> requiresRebuild = null,
            ushort templateSize = 3)
        {
            Init(
                ConvertBuildTemplate(constructInstance, buildTemplate),
                constructInstance,
                name,
                directory,
                requiresRebuild,
                templateSize);
        }

        static Action<SqlConnection, DbContextOptionsBuilder<TDbContext>> ConvertBuildTemplate(
            Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance,
            Action<TDbContext> buildTemplate)
        {
            return (connection, builder) =>
            {
                using (var dbContext = constructInstance(builder))
                {
                    if (buildTemplate == null)
                    {
                        dbContext.Database.EnsureCreated();
                    }
                    else
                    {
                        buildTemplate(dbContext);
                    }
                }
            };
        }

        public SqlInstance(
            Action<SqlConnection, DbContextOptionsBuilder<TDbContext>> buildTemplate,
            Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance,
            string instanceSuffix = null,
            Func<TDbContext, bool> requiresRebuild = null,
            ushort templateSize = 3)
        {
            var instanceName = GetInstanceName(instanceSuffix);
            var directory = DirectoryFinder.Find(instanceName);
            Init(buildTemplate, constructInstance, instanceName, directory, requiresRebuild, templateSize);
        }

        public SqlInstance(
            Action<SqlConnection, DbContextOptionsBuilder<TDbContext>> buildTemplate,
            Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance,
            string name,
            string directory,
            Func<TDbContext, bool> requiresRebuild = null,
            ushort templateSize = 3)
        {
            Init(buildTemplate, constructInstance, name, directory, requiresRebuild, templateSize);
        }

        void Init(
            Action<SqlConnection, DbContextOptionsBuilder<TDbContext>> buildTemplate,
            Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance,
            string name,
            string directory,
            Func<TDbContext, bool> requiresRebuild,
            ushort templateSize)
        {
            Guard.AgainstNullWhiteSpace(nameof(directory), directory);
            Guard.AgainstNullWhiteSpace(nameof(name), name);
            Guard.AgainstNull(nameof(constructInstance), constructInstance);
            this.constructInstance = constructInstance;
            try
            {
                var stopwatch = Stopwatch.StartNew();
                InnerInit(buildTemplate, name, directory, requiresRebuild, templateSize);
                Trace.WriteLine($"SqlInstance initialization: {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception exception)
            {
               throw ExceptionBuilder.WrapLocalDbFailure(name, directory, exception);
            }
        }

        void InnerInit(
            Action<SqlConnection, DbContextOptionsBuilder<TDbContext>> buildTemplate,
            string name,
            string directory,
            Func<TDbContext, bool> requiresRebuild,
            ushort templateSize)
        {
            Func<SqlConnection, bool> requiresRebuild2 = null;
            if (requiresRebuild != null)
            {
                requiresRebuild2 = templateConnection =>
                {
                    var builder = new DbContextOptionsBuilder<TDbContext>();
                    builder.UseSqlServer(templateConnection);
                    using (var dbContext = constructInstance(builder))
                    {
                        return requiresRebuild(dbContext);
                    }
                };
            }

            Action<SqlConnection> buildTemplate2 = connection =>
            {
                var builder = DefaultOptionsBuilder.Build<TDbContext>();
                builder.UseSqlServer(connection);
                buildTemplate(connection, builder);
            };

            var timestamp = typeof(TDbContext).Assembly.LastModified();

            wrapper = new Wrapper(name, directory, templateSize);

            wrapper.Start(requiresRebuild2, timestamp, buildTemplate2);
        }

        static string GetInstanceName(string scopeSuffix)
        {
            Guard.AgainstWhiteSpace(nameof(scopeSuffix), scopeSuffix);

            #region GetInstanceName

            if (scopeSuffix == null)
            {
                return typeof(TDbContext).Name;
            }

            return $"{typeof(TDbContext).Name}_{scopeSuffix}";

            #endregion
        }

        public void Cleanup()
        {
            wrapper.DeleteInstance();
        }

        Task<string> BuildDatabase(string dbName)
        {
            return wrapper.CreateDatabaseFromTemplate(dbName);
        }

        /// <summary>
        ///   Build DB with a name based on the calling Method.
        /// </summary>
        /// <param name="data">The seed data.</param>
        /// <param name="testFile">The path to the test class. Used to make the db name unique per test type.</param>
        /// <param name="databaseSuffix">For Xunit theories add some text based on the inline data to make the db name unique.</param>
        /// <param name="memberName">Used to make the db name unique per method. Will default to the caller method name is used.</param>
        public Task<SqlDatabase<TDbContext>> Build(
            IEnumerable<object> data,
            [CallerFilePath] string testFile = null,
            string databaseSuffix = null,
            [CallerMemberName] string memberName = null)
        {
            Guard.AgainstNullWhiteSpace(nameof(testFile), testFile);
            Guard.AgainstNullWhiteSpace(nameof(memberName), memberName);
            Guard.AgainstWhiteSpace(nameof(databaseSuffix), databaseSuffix);

            var testClass = Path.GetFileNameWithoutExtension(testFile);

            var dbName = DbNamer.DeriveDbName(databaseSuffix, memberName, testClass);
            return Build(dbName, data);
        }

        #region EfBuildSignature

        /// <summary>
        ///   Build DB with a name based on the calling Method.
        /// </summary>
        /// <param name="testFile">The path to the test class. Used to make the db name unique per test type.</param>
        /// <param name="databaseSuffix">For Xunit theories add some text based on the inline data to make the db name unique.</param>
        /// <param name="memberName">Used to make the db name unique per method. Will default to the caller method name is used.</param>
        public Task<SqlDatabase<TDbContext>> Build(
            [CallerFilePath] string testFile = null,
            string databaseSuffix = null,
            [CallerMemberName] string memberName = null)
            #endregion
        {
            return Build(null, testFile, databaseSuffix, memberName);
        }

        public async Task<SqlDatabase<TDbContext>> Build(
            string dbName,
            IEnumerable<object> data)
        {
            Guard.AgainstNullWhiteSpace(nameof(dbName), dbName);
            var connection = await BuildDatabase(dbName);
            var database = new SqlDatabase<TDbContext>(connection, constructInstance, () => wrapper.DeleteDatabase(dbName), data);
            await database.Start();
            return database;
        }

        public Task<SqlDatabase<TDbContext>> Build(string dbName)
        {
            return Build(dbName, (IEnumerable<object>) null);
        }

        public string MasterConnectionString
        {
            get => wrapper.MasterConnectionString;
        }
    }
}