using System;
using System.Data.SqlClient;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EFLocalDb
{
    public class SqlInstance<TDbContext>
        where TDbContext : DbContext
    {
        Wrapper wrapper;
        Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance;

        public SqlInstance(
            Action<SqlConnection, DbContextOptionsBuilder<TDbContext>> buildTemplate,
            Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance,
            string instanceSuffix = null, 
            Func<TDbContext,bool> requiresRebuild = null)
        {
            Guard.AgainstWhiteSpace(nameof(instanceSuffix), instanceSuffix);
            var instanceName = GetInstanceName(instanceSuffix);
            var directory = DirectoryFinder.Find(instanceName);
            Init(buildTemplate, constructInstance, instanceName, directory, requiresRebuild);
        }

        public SqlInstance(
            Action<SqlConnection, DbContextOptionsBuilder<TDbContext>> buildTemplate,
            Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance,
            string instanceName,
            string directory,
            Func<TDbContext, bool> requiresRebuild = null)
        {
            Guard.AgainstNullWhiteSpace(nameof(directory), directory);
            Guard.AgainstNullWhiteSpace(nameof(instanceName), instanceName);
            Guard.AgainstNull(nameof(buildTemplate), buildTemplate);
            Guard.AgainstNull(nameof(constructInstance), constructInstance);
            Init(buildTemplate, constructInstance, instanceName, directory, requiresRebuild);
        }

        void Init(
            Action<SqlConnection, DbContextOptionsBuilder<TDbContext>> buildTemplate,
            Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance,
            string instanceName, 
            string directory,
            Func<TDbContext, bool> requiresRebuild)
        {
            try
            {
                wrapper = new Wrapper(instanceName, directory);

                this.constructInstance = constructInstance;

                wrapper.Start();

                if (requiresRebuild != null)
                {
                    if (wrapper.DatabaseFileExists("template"))
                    {
                        var connection = wrapper.CreateDatabaseFromFile("template").GetAwaiter().GetResult();
                        
                        var builder = new DbContextOptionsBuilder<TDbContext>();
                        builder.UseSqlServer(connection);
                        using (var dbContext = constructInstance(builder))
                        {
                            if (!requiresRebuild(dbContext))
                            {
                                wrapper.Detach("template");
                                wrapper.Purge();
                                wrapper.DeleteFiles(exclude: "template");
                                return;
                            }
                        }
                    }
                }

                wrapper.Purge();
                wrapper.DeleteFiles();

                var connectionString = wrapper.CreateDatabase("template");
                // needs to be pooling=false so that we can immediately detach and use the files
                connectionString += ";Pooling=false";
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    var builder = new DbContextOptionsBuilder<TDbContext>();
                    builder.ConfigureWarnings(warnings => warnings.Throw(CoreEventId.IncludeIgnoredWarning));
                    builder.UseSqlServer(connection);
                    buildTemplate(connection, builder);
                }

                wrapper.Detach("template");
            }
            catch (Exception exception)
            {
                var message = $@"Failed to setup a LocalDB instance.
{nameof(instanceName)}: {instanceName}
{nameof(directory)}: {directory}:

To cleanup perform the following actions:
 * Execute 'sqllocaldb stop {instanceName}'
 * Execute 'sqllocaldb delete {instanceName}'
 * Delete the directory {directory}'
";
                throw new Exception(message, exception);
            }
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

        Task<string> BuildContext(string dbName)
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
        public Task<SqlDatabase<TDbContext>> Build(
            [CallerFilePath] string testFile = null,
            string databaseSuffix = null,
            [CallerMemberName] string memberName = null)
        {
            #endregion
            Guard.AgainstNullWhiteSpace(nameof(testFile), testFile);
            Guard.AgainstNullWhiteSpace(nameof(memberName), memberName);
            Guard.AgainstWhiteSpace(nameof(databaseSuffix), databaseSuffix);

            var testClass = Path.GetFileNameWithoutExtension(testFile);

            #region DeriveName

            var dbName = $"{testClass}_{memberName}";
            if (databaseSuffix != null)
            {
                dbName = $"{dbName}_{databaseSuffix}";
            }

            #endregion

            return Build(dbName);
        }

        public async Task<SqlDatabase<TDbContext>> Build(string dbName)
        {
            Guard.AgainstNullWhiteSpace(nameof(dbName), dbName);
            return new SqlDatabase<TDbContext>(await BuildContext(dbName), constructInstance);
        }
    }
}