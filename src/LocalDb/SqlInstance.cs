using System;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace LocalDb
{
    public class SqlInstance
    {
        Wrapper wrapper;

        public string ServerName => wrapper.ServerName;

        public SqlInstance(
            string name,
            Action<SqlConnection> buildTemplate,
            string directory = null,
            DateTime? timestamp = null,
            ushort templateSize = 3)
        {
            Guard.AgainstNull(nameof(buildTemplate), buildTemplate);
            Guard.AgainstWhiteSpace(nameof(directory), directory);
            Guard.AgainstNullWhiteSpace(nameof(name), name);
            if (directory == null)
            {
                directory = DirectoryFinder.Find(name);
            }
            DateTime resultTimestamp;
            if (timestamp == null)
            {
                var callingAssembly = Assembly.GetCallingAssembly();
                resultTimestamp = Timestamp.LastModified(callingAssembly);
            }
            else
            {
                resultTimestamp = timestamp.Value;
            }
            wrapper = new Wrapper(name, directory, templateSize);
            wrapper.Start(resultTimestamp, buildTemplate);
        }

        public void Cleanup()
        {
            wrapper.DeleteInstance();
        }

        Task<string> BuildContext(string dbName)
        {
            return wrapper.CreateDatabaseFromTemplate(dbName);
        }

        #region BuildSignature
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
        #endregion
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
            var connectionString = await BuildContext(dbName);
            var sqlDatabase = new SqlDatabase(connectionString, () => wrapper.DeleteDatabase(dbName));
            await sqlDatabase.Start();
            return sqlDatabase;
        }

        public string MasterConnectionString => wrapper.MasterConnectionString;
    }
}