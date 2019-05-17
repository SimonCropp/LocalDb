using System;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace LocalDb
{
    public static class SqlInstanceService
    {
        static SqlInstance instance;

        public static string ServerName
        {
            get
            {
                AssertInstanceNotNull();
                return instance.ServerName;
            }
        }

        public static void Register(
            string name,
            Action<SqlConnection> buildTemplate,
            string directory = null,
            Func<SqlConnection, Task> constructInstance = null,
            Func<SqlConnection, bool> requiresRebuild = null)
        {
            AssertInstanceNotNull();
            instance = new SqlInstance(name, buildTemplate, directory, constructInstance, requiresRebuild);
        }

        static void AssertInstanceNotNull()
        {
            if (instance == null)
            {
                return;
            }

            throw new Exception($@"There is already an instance registered.
When using that static registration API, only one registration is allowed.
To register different configurations use the instance based api via {typeof(SqlInstance).Name}");
        }

        /// <summary>
        ///   Build DB with a name based on the calling Method.
        /// </summary>
        /// <param name="testFile">The path to the test class. Used to make the db name unique per test type.</param>
        /// <param name="databaseSuffix">For Xunit theories add some text based on the inline data to make the db name unique.</param>
        /// <param name="memberName">Used to make the db name unique per method. Will default to the caller method name is used.</param>
        public static Task<SqlDatabase> Build(
            [CallerFilePath] string testFile = null,
            string databaseSuffix = null,
            [CallerMemberName] string memberName = null)
        {
            return instance.Build(testFile, databaseSuffix, memberName);
        }

        public static Task<SqlDatabase> Build(string dbName)
        {
            return instance.Build(dbName);
        }
    }
}