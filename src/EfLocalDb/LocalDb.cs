using System;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EFLocalDb
{
    public static class LocalDb<TDbContext>
        where TDbContext : DbContext
    {
        static SqlInstance<TDbContext> instance;

        public static void Register(
            Action<SqlConnection, DbContextOptionsBuilder<TDbContext>> buildTemplate,
            Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance,
            string instanceSuffix = null)
        {
            instance = new SqlInstance<TDbContext>(buildTemplate, constructInstance, instanceSuffix);
        }

        public static void Register(
            Action<SqlConnection, DbContextOptionsBuilder<TDbContext>> buildTemplate,
            Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance,
            string instanceName,
            string directory)
        {
            instance = new SqlInstance<TDbContext>(buildTemplate, constructInstance, instanceName, directory);
        }

        /// <summary>
        ///   Build DB with a name based on the calling Method.
        /// </summary>
        /// <param name="caller">Used to make the db name unique per type. Normally pass this.</param>
        /// <param name="databaseSuffix">For Xunit theories add some text based on the inline data to make the db name unique.</param>
        /// <param name="memberName">Used to make the db name unique per method. Will default to the caller method name is used.</param>
        public static Task<SqlDatabase<TDbContext>> Build(
            object caller,
            string databaseSuffix = null,
            [CallerMemberName] string memberName = null)
        {
            return instance.Build(caller, databaseSuffix, memberName);
        }

        public static Task<SqlDatabase<TDbContext>> Build(string dbName)
        {
            return instance.Build(dbName);
        }
    }
}