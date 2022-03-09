static class SqlBuilder
{
    public static string GetCreateOrMakeOnlineCommand(string name, string dataFile, string logFile)
    {
        var createCommand = $@"
create database [{name}] on
(
    name = [{name}],
    filename = '{dataFile}'
),
(
    filename = '{logFile}'
)
for attach;";

        return $@"
if db_id('{name}') is null
    begin
       {createCommand}
    end;
else
    begin
        alter database [{name}] set online;
    end;
alter database [{name}] set read_write;
"
            ;
    }

    public static string DetachTemplateCommand = @"
if db_id('template') is not null
begin
    alter database [template] set single_user with rollback immediate;
    execute sp_detach_db 'template', 'true';
end;";

    public static string GetOptimizationCommand(ushort size) =>
        $@"
execute sp_configure 'show advanced options', 1;
reconfigure;
execute sp_configure 'user instance timeout', 30;
reconfigure;

-- begin-snippet: ShrinkModelDb
use model;
dbcc shrinkfile(modeldev, {size})
-- end-snippet
";

    public static string GetCreateTemplateCommand(string dataFile, string logFile) =>
        $@"
if db_id('template') is not null
begin
  execute sp_detach_db 'template', 'true';
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
";

    public static string BuildDeleteDbCommand(string dbName) =>
        $@"
alter database [{dbName}] set single_user with rollback immediate;
drop database [{dbName}];";

    public static string TakeDbsOfflineCommand = @"
declare @command nvarchar(max)
set @command = ''

select @command = @command
+ '
    alter database [' + [name] + '] set single_user with rollback immediate;
    alter database [' + [name] + '] set multi_user;
    alter database [' + [name] + '] set offline;
'
from master.sys.databases
where [name] not in ('master', 'model', 'msdb', 'tempdb', 'template');
execute sp_executesql @command";
}