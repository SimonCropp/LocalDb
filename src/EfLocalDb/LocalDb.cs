using System;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EFLocalDb
{
    public class LocalDb<T>
        where T : DbContext
    {
        static LocalDbWrapper localDbWrapper;
        static Func<DbContextOptionsBuilder<T>, T> constructInstance;

        public static void Init(
            Action<SqlConnection, DbContextOptionsBuilder<T>> buildTemplate,
            Func<DbContextOptionsBuilder<T>, T> constructInstance,
            string scopeSuffix = null)
        {
            string scope;
            if (scopeSuffix == null)
            {
                scope = typeof(T).Name;
            }
            else
            {
                scope = typeof(T).Name + "_" + scopeSuffix;
            }
            var dataDirectory = DataDirectoryFinder.Find(scope);
            localDbWrapper = new LocalDbWrapper(scope, dataDirectory);
            LocalDb<T>.constructInstance = constructInstance;
            localDbWrapper.ResetLocalDb();

            var connectionString = localDbWrapper.CreateDatabase("template");
            // needs to be pooling=false so that we can immediately detach and use the files
            connectionString += ";Pooling=false";
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var builder = new DbContextOptionsBuilder<T>();
                builder.ConfigureWarnings(warnings => warnings.Throw(CoreEventId.IncludeIgnoredWarning));
                builder.UseSqlServer(connection);
                buildTemplate(connection, builder);
            }

            localDbWrapper.Detach("template");
        }

        static Task<string> BuildContext(string dbName)
        {
            return localDbWrapper.CreateDatabaseFromTemplate(dbName, "template");
        }

        public string ConnectionString { get; private set; }

        /// <summary>
        ///   Build DB with a name based on the calling Method
        /// </summary>
        /// <param name="caller">Normally pass this </param>
        /// <param name="suffix">For Xunit theories add some text based on the inline data to make the db name unique</param>
        /// <param name="memberName">do not use, will default to the caller method name is used</param>
        public static async Task<LocalDb<T>> Build(object caller, string suffix = null, [CallerMemberName] string memberName = null)
        {
            var type = caller.GetType();
            var dbName = $"{type.Name}_{memberName}";
            if (suffix != null)
            {
                dbName = $"{dbName}_{suffix}";
            }

            return new LocalDb<T>
            {
                ConnectionString = await BuildContext(dbName)
            };
        }

        public async Task AddSeed(params object[] entities)
        {
            using (var seedingDataContext = NewDataContext())
            {
                seedingDataContext.AddRange(entities);
                await seedingDataContext.SaveChangesAsync();
            }
        }

        public T NewDataContext()
        {
            var builder = new DbContextOptionsBuilder<T>();
            builder.UseSqlServer(ConnectionString);
            return constructInstance(builder);
        }
    }
}