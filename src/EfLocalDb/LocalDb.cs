using System;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EFLocalDb
{
    public class LocalDb<TDbContext>
        where TDbContext : DbContext
    {
        static LocalDbWrapper localDbWrapper;
        static Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance;

        public static void Register(
            Action<SqlConnection, DbContextOptionsBuilder<TDbContext>> buildTemplate,
            Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance,
            string scopeSuffix = null)
        {
            Guard.AgainstWhiteSpace(nameof(scopeSuffix), scopeSuffix);
            var instanceName = GetInstanceName(scopeSuffix);
            var directory = DirectoryFinder.Find(instanceName);
            Register(buildTemplate, constructInstance, instanceName, directory);
        }

        public static void Register(
            Action<SqlConnection, DbContextOptionsBuilder<TDbContext>> buildTemplate,
            Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance,
            string instanceName,
            string directory)
        {
            Guard.AgainstNullWhiteSpace(nameof(directory), directory);
            Guard.AgainstNullWhiteSpace(nameof(instanceName), instanceName);
            Guard.AgainstNull(nameof(buildTemplate), buildTemplate);
            Guard.AgainstNull(nameof(constructInstance), constructInstance);
            try
            {
                localDbWrapper = new LocalDbWrapper(instanceName, directory);

                LocalDb<TDbContext>.constructInstance = constructInstance;
                localDbWrapper.Start();
                localDbWrapper.Purge();
                localDbWrapper.DeleteFiles();

                var connectionString = localDbWrapper.CreateDatabase("template");
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

                localDbWrapper.Detach("template");
            }
            catch (Exception exception)
            {
                var message = $@"Failed to setup a LocalDB instance named {instanceName}.
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

        public static void Cleanup()
        {
            localDbWrapper.DeleteInstance();
        }

        static Task<string> BuildContext(string dbName)
        {
            return localDbWrapper.CreateDatabaseFromTemplate(dbName, "template");
        }

        public string ConnectionString { get; private set; }

        #region BuildLocalDbSignature

        /// <summary>
        ///   Build DB with a name based on the calling Method.
        /// </summary>
        /// <param name="caller">Used to make the db name unique per type. Normally pass this.</param>
        /// <param name="suffix">For Xunit theories add some text based on the inline data to make the db name unique.</param>
        /// <param name="memberName">Used to make the db name unique per method. Will default to the caller method name is used.</param>
        public static Task<LocalDb<TDbContext>> Build(
            object caller,
            string suffix = null,
            [CallerMemberName] string memberName = null)
        {
            #endregion

            Guard.AgainstNull(nameof(caller), caller);
            Guard.AgainstNullWhiteSpace(nameof(memberName), memberName);
            Guard.AgainstWhiteSpace(nameof(suffix), suffix);

            #region DeriveName

            var type = caller.GetType();
            var dbName = $"{type.Name}_{memberName}";
            if (suffix != null)
            {
                dbName = $"{dbName}_{suffix}";
            }

            #endregion

            return Build(dbName);
        }

        public static async Task<LocalDb<TDbContext>> Build(string dbName)
        {
            Guard.AgainstNullWhiteSpace(nameof(dbName), dbName);
            return new LocalDb<TDbContext>
            {
                ConnectionString = await BuildContext(dbName)
            };
        }

        public async Task AddSeed(params object[] entities)
        {
            using (var dbContext = NewDbContext())
            {
                dbContext.AddRange(entities);
                await dbContext.SaveChangesAsync();
            }
        }

        public TDbContext NewDbContext()
        {
            var builder = new DbContextOptionsBuilder<TDbContext>();
            builder.UseSqlServer(ConnectionString);
            return constructInstance(builder);
        }
    }
}