using System;
using System.Data.Common;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

static class SqlExtensions
{
    public static async Task ExecuteCommandAsync(this DbConnection connection, string commandText)
    {
        commandText = commandText.Trim();

        try
        {
            var stopwatch = Stopwatch.StartNew();

#if(NETSTANDARD2_1)
            await using (var command = connection.CreateCommand())
#else
            using (var command = connection.CreateCommand())
#endif
            {
                command.CommandText = commandText;
                await command.ExecuteNonQueryAsync();
            }

            if (LocalDbLogging.SqlLoggingEnabled)
            {
                LocalDbLogging.Log($@"Executed SQL ({stopwatch.ElapsedMilliseconds}.ms):
{commandText.IndentLines()}");
            }
        }
        catch (DbException exception)
        {
            throw BuildException(connection, commandText, exception);
        }
        catch (Exception exception)
        {
            throw BuildException(connection, commandText, exception);
        }
    }

    static Exception BuildException(DbConnection connection, string commandText, Exception exception)
    {
        var builder = new StringBuilder($@"Failed to execute SQL command.
{nameof(commandText)}: {commandText}
connectionString: {connection.ConnectionString}
");

        return new Exception(builder.ToString(), exception);
    }
}