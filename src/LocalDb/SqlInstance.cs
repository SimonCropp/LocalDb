using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace LocalDb
{
    public class SqlInstance
    {
        Wrapper wrapper;
        Func<SqlConnection, Task> constructInstance;
        
        public string ServerName => wrapper.ServerName;

        public SqlInstance(
            Action<SqlConnection> buildTemplate,
            string name,
            Func<SqlConnection, Task> constructInstance = null,
            Func<SqlConnection, bool> requiresRebuild = null)
        {
            Guard.AgainstWhiteSpace(nameof(name), name);
            var directory = DirectoryFinder.Find(name);
            Init(buildTemplate, constructInstance, name, directory, requiresRebuild);
        }

        public SqlInstance(Action<SqlConnection> buildTemplate,
            string name,
            string directory,
            Func<SqlConnection, Task> constructInstance = null,
            Func<SqlConnection, bool> requiresRebuild = null)
        {
            Guard.AgainstNullWhiteSpace(nameof(directory), directory);
            Guard.AgainstNullWhiteSpace(nameof(name), name);
            Guard.AgainstNull(nameof(buildTemplate), buildTemplate);
            Init(buildTemplate, constructInstance, name, directory, requiresRebuild);
        }

        void Init(
            Action<SqlConnection> buildTemplate,
            Func<SqlConnection, Task> constructInstance,
            string name,
            string directory,
            Func<SqlConnection, bool> requiresRebuild)
        {
            try
            {
                wrapper = new Wrapper(name, directory);
                
                Trace.WriteLine($@"Creating LocalDb instance.
Server Name: {ServerName}");

                if (constructInstance == null)
                {
                    this.constructInstance = connection => Task.CompletedTask;
                }
                else
                {
                    this.constructInstance = constructInstance;
                }

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
                    buildTemplate(connection);
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

        bool CheckRequiresRebuild(Func<SqlConnection, bool> requiresRebuild)
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
            bool rebuild;
            using (var sqlConnection = new SqlConnection(connection))
            {
                rebuild = requiresRebuild(sqlConnection);
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
        
        public void Cleanup()
        {
            wrapper.DeleteInstance();
        }

        Task<string> BuildContext(string dbName)
        {
            return wrapper.CreateDatabaseFromTemplate(dbName, "template");
        }

        /// <summary>
        ///   Build DB with a name based on the calling Method.
        /// </summary>
        /// <param name="testFile">The path to the test class. Used to make the db name unique per test type.</param>
        /// <param name="databaseSuffix">For Xunit theories add some text based on the inline data to make the db name unique.</param>
        /// <param name="memberName">Used to make the db name unique per method. Will default to the caller method name is used.</param>
        public Task<SqlDatabase> Build(
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

        public async Task<SqlDatabase> Build(string dbName)
        {
            Guard.AgainstNullWhiteSpace(nameof(dbName), dbName);
            var sqlDatabase = new SqlDatabase(await BuildContext(dbName));
            using (var sqlConnection = new SqlConnection(sqlDatabase.Connection))
            {
                await sqlConnection.OpenAsync();
                await constructInstance(sqlConnection);
            }

            return sqlDatabase;
        }
    }
}