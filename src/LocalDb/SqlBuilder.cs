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

    public static string DetachAndShrinkTemplateCommand =
        """
        use [template];
        dbcc shrinkfile(template);
        dbcc shrinkfile(template_log, 1);
        use master;
        alter database [template] set single_user with rollback immediate;
        execute sp_detach_db N'template', 'true';
        """;

    public static string GetOptimizeModelDbCommand(ushort size, ushort shutdownTimeout) =>
        $"""
         execute sp_configure 'show advanced options', 1;
         reconfigure;
         execute sp_configure 'user instance timeout', {shutdownTimeout};
         reconfigure;

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
