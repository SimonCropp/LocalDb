using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EfLocalDb
{
    public static class SqlInstanceService<TDbContext>
        where TDbContext : DbContext
    {
        static SqlInstance<TDbContext> instance;

        public static string ServerName
        {
            get
            {
                ThrowIfInstanceNull();
                return instance.ServerName;
            }
        }

        public static void Register(
            Action<SqlConnection, DbContextOptionsBuilder<TDbContext>> buildTemplate,
            Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance,
            string instanceSuffix = null,
            Func<TDbContext, bool> requiresRebuild = null,
            ushort templateSize = 3)
        {
            ThrowIfInstanceNotNull();
            instance = new SqlInstance<TDbContext>(buildTemplate, constructInstance, instanceSuffix, requiresRebuild, templateSize);
        }

        public static void Register(
            Action<SqlConnection, DbContextOptionsBuilder<TDbContext>> buildTemplate,
            Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance,
            string instanceName,
            string directory,
            Func<TDbContext, bool> requiresRebuild = null,
            ushort templateSize = 3)
        {
            ThrowIfInstanceNotNull();
            instance = new SqlInstance<TDbContext>(buildTemplate, constructInstance, instanceName, directory, requiresRebuild, templateSize);
        }

        public static void Register(
            Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance,
            Action<TDbContext> buildTemplate = null,
            string instanceSuffix = null,
            Func<TDbContext, bool> requiresRebuild = null,
            ushort templateSize = 3)
        {
            ThrowIfInstanceNotNull();
            instance = new SqlInstance<TDbContext>(constructInstance, buildTemplate, instanceSuffix, requiresRebuild, templateSize);
        }

        public static void Register(
            Func<DbContextOptionsBuilder<TDbContext>, TDbContext> constructInstance,
            string instanceName,
            string directory,
            Action<TDbContext> buildTemplate = null,
            Func<TDbContext, bool> requiresRebuild = null,
            ushort templateSize = 3)
        {
            ThrowIfInstanceNotNull();
            instance = new SqlInstance<TDbContext>(constructInstance, instanceName, directory, buildTemplate, requiresRebuild, templateSize);
        }

        static void ThrowIfInstanceNull()
        {
            if (instance == null)
            {
                throw new Exception(@"There is no instance registered.
Ensure that `SqlInstanceService.Register` has been called.");
            }
        }

        static void ThrowIfInstanceNotNull()
        {
            if (instance != null)
            {
                throw new Exception($@"There is already an instance registered for `TDbContext`.
When using that static registration API, only one registration is allowed per DBContext type.
To register different configurations for the same DbContext type use the instance based api via `SqlInstance<TDbContext>)`.");
            }
        }

        /// <summary>
        ///   Build DB with a name based on the calling Method.
        /// </summary>
        /// <param name="testFile">The path to the test class. Used to make the db name unique per test type.</param>
        /// <param name="databaseSuffix">For Xunit theories add some text based on the inline data to make the db name unique.</param>
        /// <param name="memberName">Used to make the db name unique per method. Will default to the caller method name is used.</param>
        public static Task<SqlDatabase<TDbContext>> Build(
            [CallerFilePath] string testFile = null,
            string databaseSuffix = null,
            [CallerMemberName] string memberName = null)
        {
            ThrowIfInstanceNull();
            return instance.Build(testFile, databaseSuffix, memberName);
        }

        /// <summary>
        ///   Build DB with a name based on the calling Method.
        /// </summary>
        /// <param name="testFile">The path to the test class. Used to make the db name unique per test type.</param>
        /// <param name="databaseSuffix">For Xunit theories add some text based on the inline data to make the db name unique.</param>
        /// <param name="memberName">Used to make the db name unique per method. Will default to the caller method name is used.</param>
        public static Task<SqlDatabase<TDbContext>> Build(
            IEnumerable<object> data,
            [CallerFilePath] string testFile = null,
            string databaseSuffix = null,
            [CallerMemberName] string memberName = null)
        {
            ThrowIfInstanceNull();
            return instance.Build(data, testFile, databaseSuffix, memberName);
        }

        public static Task<SqlDatabase<TDbContext>> Build(
            string dbName,
            IEnumerable<object> data)
        {
            ThrowIfInstanceNull();
            return instance.Build(data, dbName);
        }
        public static Task<SqlDatabase<TDbContext>> Build(
            string dbName)
        {
            ThrowIfInstanceNull();
            return instance.Build(dbName);
        }
    }
}