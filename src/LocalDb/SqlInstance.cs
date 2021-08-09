﻿using System;
using System.Data.Common;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace LocalDb
{
    public class SqlInstance
    {
        internal readonly Wrapper Wrapper;

        public string ServerName => Wrapper.ServerName;

        public SqlInstance(
            string name,
            Func<DbConnection, Task> buildTemplate,
            string? directory = null,
            DateTime? timestamp = null,
            ushort templateSize = 3,
            ExistingTemplate? exitingTemplate = null,
            Func<DbConnection, Task>? callback = null)
        {
            Guard.AgainstWhiteSpace(nameof(directory), directory);
            Guard.AgainstNullWhiteSpace(nameof(name), name);
            directory = DirectoryFinder.Find(name);
            DirectoryCleaner.CleanInstance(directory);
            var callingAssembly = Assembly.GetCallingAssembly();
            var resultTimestamp = GetTimestamp(timestamp, buildTemplate, callingAssembly);
            Wrapper = new(s => new SqlConnection(s), name, directory, templateSize, exitingTemplate, callback);
            Wrapper.Start(resultTimestamp, buildTemplate);
        }

        static DateTime GetTimestamp(DateTime? timestamp, Delegate? buildTemplate, Assembly callingAssembly)
        {
            if (timestamp is not null)
            {
                return timestamp.Value;
            }

            if (buildTemplate is not null)
            {
                return Timestamp.LastModified(buildTemplate);
            }

            return Timestamp.LastModified(callingAssembly);
        }

        public void Cleanup()
        {
            Wrapper.DeleteInstance();
        }

        Task<string> BuildContext(string dbName)
        {
            return Wrapper.CreateDatabaseFromTemplate(dbName);
        }

        #region ConventionBuildSignature
        /// <summary>
        ///   Build database with a name based on the calling Method.
        /// </summary>
        /// <param name="testFile">
        /// The path to the test class.
        /// Used to make the database name unique per test type.
        /// </param>
        /// <param name="databaseSuffix">
        /// For Xunit theories add some text based on the inline data
        /// to make the db name unique.
        /// </param>
        /// <param name="memberName">
        /// Used to make the db name unique per method.
        /// Will default to the caller method name is used.
        /// </param>
        public Task<SqlDatabase> Build(
            [CallerFilePath] string testFile = "",
            string? databaseSuffix = null,
            [CallerMemberName] string memberName = "")
        #endregion
        {
            Guard.AgainstNullWhiteSpace(nameof(testFile), testFile);
            Guard.AgainstNullWhiteSpace(nameof(memberName), memberName);
            Guard.AgainstWhiteSpace(nameof(databaseSuffix), databaseSuffix);

            var testClass = Path.GetFileNameWithoutExtension(testFile);

            var name = DbNamer.DeriveDbName(databaseSuffix, memberName, testClass);

            return Build(name);
        }

        #region ExplicitBuildSignature
        /// <summary>
        ///   Build database with an explicit name.
        /// </summary>
        public async Task<SqlDatabase> Build(string dbName)
        #endregion
        {
            Guard.AgainstNullWhiteSpace(nameof(dbName), dbName);
            var connection = await BuildContext(dbName);
            SqlDatabase database = new(
                connection,
                dbName,
                () => Wrapper.DeleteDatabase(dbName));
            await database.Start();
            return database;
        }

        public string MasterConnectionString => Wrapper.MasterConnectionString;
    }
}