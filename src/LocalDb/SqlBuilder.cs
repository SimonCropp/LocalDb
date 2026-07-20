static class SqlBuilder
{
    public static string GetCreateOrMakeOnlineCommand(string name, string dataFile, string logFile)
    {
        var createCommand = $"""
                             create database [{name}] on
                             (
                                 name = [{name}],
                                 filename = '{dataFile}'
                             ),
                             (
                                 filename = '{logFile}'
                             )
                             for attach;
                             """;

        return $"""
                if db_id(N'{name}') is null
                    begin
                {createCommand}
                    end;
                else
                    begin
                        alter database [{name}] set online;
                    end;
                alter database [{name}] set read_write;
                """;
    }

    public static string GetAttachTemplateCommand(string dataFile, string logFile) =>
        $"""
         if db_id(N'template') is null
         begin
             create database [template] on
             (
                 name = template,
                 filename = '{dataFile}'
             ),
             (
                 filename = '{logFile}'
             )
             for attach;
         end;
         """;

    public static string DetachTemplateCommand =
        """
        use master;
        alter database [template] set single_user with rollback immediate;
        execute sp_detach_db N'template', 'true';
        """;

    // Database-level settings applied to the template before detach. Persisted in the
    // .mdf/.ldf files, so every database attached from the template inherits them.
    //   auto_update_statistics off  - avoids background stats updates causing
    //                                 nondeterministic test timing.
    //   delayed_durability forced   - commits return without waiting for the log flush. Test
    //                                 databases are disposable, so crash durability of the
    //                                 last commits is worthless, while test workloads are
    //                                 thousands of tiny commits. Benchmark
    //                                 (DelayedDurabilityBenchmarks): autocommit inserts are
    //                                 ~40% faster and write ~70% less log I/O.
    //   read_committed_snapshot on  - READ COMMITTED uses row versioning instead of
    //                                 shared locks, preventing S/X-lock deadlocks
    //                                 between parallel [SharedDbWithTransaction] tests
    //                                 against the same shared database.
    // read_committed_snapshot requires exclusive access to the database, so it uses
    // "with rollback immediate" to evict any sessions a buildTemplate/callback left
    // open (e.g. an SMO ServerConnection). Without it the statement blocks on those
    // sessions until the command timeout expires.
    // Note: auto_close cannot be fixed here — "create database ... for attach" resets it to the
    // model default (on), so a template-level setting never reaches the attached copies. See
    // OpenSharedDatabase, which turns it off for the one database that reopens repeatedly.
    public static string TemplateSettingsCommand =
        """
        alter database [template] set auto_update_statistics off;
        alter database [template] set delayed_durability = forced;
        alter database [template] set read_committed_snapshot on with rollback immediate;
        """;

    public static string DetachAndShrinkTemplateCommand =
        """
        use [template];
        dbcc shrinkfile(template);
        dbcc shrinkfile(template_log, 1);
        use master;
        alter database [template] set single_user with rollback immediate;
        execute sp_detach_db N'template', 'true';
        """;

    // The default trace and the system_health session write diagnostics for the life of the
    // instance into the directory LocalDB owns, which is never cleaned while the instance
    // exists. Measured across ~280 instances they account for 18% of the on-disk size, and
    // neither is of much use for a throwaway test instance. This is not a speed up:
    // both are written in the background rather than on the startup path, and disabling
    // them made no measurable difference to instance start time.
    // hkenginexesession, the other event session that writes here, is owned by the XTP
    // engine, is absent from sys.server_event_sessions, and cannot be altered.
    public static string GetOptimizeModelDbCommand(ushort size, ushort shutdownTimeout) =>
        $"""
         execute sp_configure 'show advanced options', 1;
         reconfigure;
         execute sp_configure 'user instance timeout', {shutdownTimeout};
         reconfigure;
         execute sp_configure 'default trace enabled', 0;
         reconfigure;

         if exists (select * from sys.dm_xe_sessions where name = 'system_health')
         begin
             alter event session system_health on server state = stop;
         end;

         if exists (select * from sys.server_event_sessions where name = 'system_health')
         begin
             alter event session system_health on server with (startup_state = off);
         end;

         -- begin-snippet: ShrinkModelDb
         use model;
         dbcc shrinkfile(modeldev, {size})
         -- end-snippet
         """;

    public static string GetCreateTemplateCommand(string dataFile, string logFile) =>
        $"""
         if db_id(N'template') is not null
         begin
           execute sp_detach_db N'template', 'true';
         end;
         create database template on
         (
             name = template,
             filename = '{dataFile}',
             fileGrowth = 100KB
         )
         log on
         (
             name = template_log,
             filename = '{logFile}',
             size = 512KB,
             filegrowth = 100KB
         );
         -- mixed_page_allocation on lets small objects share mixed extents instead of
         -- each index reserving a dedicated 64KB uniform extent. Test schemas are usually
         -- many small tables, so with the SQL Server default (off) most of the seeded
         -- template is empty extents. Setting this before the schema is built keeps the
         -- template (and therefore every per-test database cloned from it) far smaller:
         -- a representative 80-temporal-table schema drops from ~26MB to ~8MB.
         alter database template set mixed_page_allocation on;
         """;

    public static string BuildDeleteDbCommand(string dbName) =>
        $"""
         alter database [{dbName}] set single_user with rollback immediate;
         drop database [{dbName}];
         """;

    public static string GetTakeDbsOfflineCommand(string name) =>
        $"""
         if db_id(N'{name}') is not null
           alter database [{name}] set offline with rollback immediate;
         """;
}
