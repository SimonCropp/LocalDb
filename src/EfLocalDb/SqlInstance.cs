using System;
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
            Action<SqlConnection, DbContextOptionsBuilder<TDbContext>> buildTemplate,
            Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance,
            string instanceSuffix = null,
            Func<TDbContext, bool> requiresRebuild = null)
        {
            Guard.AgainstWhiteSpace(nameof(instanceSuffix), instanceSuffix);
            var instanceName = GetInstanceName(instanceSuffix);
            var directory = DirectoryFinder.Find(instanceName);
            Init(buildTemplate, constructInstance, instanceName, directory, requiresRebuild);
        }

        public SqlInstance(
            Action<SqlConnection, DbContextOptionsBuilder<TDbContext>> buildTemplate,
            Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance,
            string name,
            string directory,
            Func<TDbContext, bool> requiresRebuild = null)
        {
            Guard.AgainstNullWhiteSpace(nameof(directory), directory);
            Guard.AgainstNullWhiteSpace(nameof(name), name);
            Guard.AgainstNull(nameof(buildTemplate), buildTemplate);
            Guard.AgainstNull(nameof(constructInstance), constructInstance);
            Init(buildTemplate, constructInstance, name, directory, requiresRebuild);
        }

        void Init(
            Action<SqlConnection, DbContextOptionsBuilder<TDbContext>> buildTemplate,
            Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance,
            string name,
            string directory,
            Func<TDbContext, bool> requiresRebuild)
        {
            try
            {
                wrapper = new Wrapper(name, directory);

                Trace.WriteLine($@"Creating LocalDb instance.
Server Name: {ServerName}");

                this.constructInstance = constructInstance;

                wrapper.Start();

                if (!CheckRequiresRebuild(requiresRebuild))
                {
                    return;
                }

                wrapper.Purge();
                wrapper.DeleteFiles();

                var connectionString = wrapper.CreateDatabase("template");
                connectionString = Wrapper.NonPooled(connectionString);
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    var builder = DefaultOptionsBuilder.Build<TDbContext>();
                    builder.UseSqlServer(connection);
                    buildTemplate(connection, builder);
                }

                wrapper.Detach("template");
            }
            catch (Exception exception)
            {
                var message = $@"Failed to setup a LocalDB instance.
{nameof(name)}: {name}
{nameof(directory)}: {directory}:

To cleanup perform the following actions:
 * Execute 'sqllocaldb stop {name}'
 * Execute 'sqllocaldb delete {name}'
 * Delete the directory {directory}'
";
                throw new Exception(message, exception);
            }
        }

        bool CheckRequiresRebuild(Func<TDbContext, bool> requiresRebuild)
        {
            if (requiresRebuild == null)
            {
                return true;
            }

            if (!wrapper.DatabaseFileExists("template"))
            {
                return true;
            }

            var connection = wrapper.CreateDatabaseFromFile("template").GetAwaiter().GetResult();
            connection = Wrapper.NonPooled(connection);
            var builder = new DbContextOptionsBuilder<TDbContext>();
            builder.UseSqlServer(connection);
            bool rebuild;
            using (var dbContext = constructInstance(builder))
            {
                rebuild = requiresRebuild(dbContext);
            }

            if (rebuild)
            {
                return true;
            }

            wrapper.Detach("template");
            wrapper.Purge();
            wrapper.DeleteFiles(exclude: "template");
            return false;
        }

        static string GetInstanceName(string scopeSuffix)
        {
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
            return wrapper.CreateDatabaseFromTemplate(dbName, "template");
        }

        #region BuildLocalDbSignature

        /// <summary>
        ///   Build DB with a name based on the calling Method.
        /// </summary>
        /// <param name="testFile">The path to the test class. Used to make the db name unique per test type.</param>
        /// <param name="databaseSuffix">For Xunit theories add some text based on the inline data to make the db name unique.</param>
        /// <param name="memberName">Used to make the db name unique per method. Will default to the caller method name is used.</param>
        #endregion
        public Task<SqlDatabase<TDbContext>> Build(
            [CallerFilePath] string testFile = null,
            string databaseSuffix = null,
            [CallerMemberName] string memberName = null)
        {
            Guard.AgainstNullWhiteSpace(nameof(testFile), testFile);
            Guard.AgainstNullWhiteSpace(nameof(memberName), memberName);
            Guard.AgainstWhiteSpace(nameof(databaseSuffix), databaseSuffix);

            var testClass = Path.GetFileNameWithoutExtension(testFile);

            var dbName = DbNamer.DeriveDbName(databaseSuffix, memberName, testClass);
            return Build(dbName);
        }

        public async Task<SqlDatabase<TDbContext>> Build(string dbName)
        {
            Guard.AgainstNullWhiteSpace(nameof(dbName), dbName);
            var connection = await BuildDatabase(dbName);
            return new SqlDatabase<TDbContext>(connection, constructInstance);
        }
    }
}