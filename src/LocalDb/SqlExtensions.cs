using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

static class SqlExtensions
{
    public static async Task ExecuteCommandAsync(this SqlConnection connection, string commandText)
    {
        commandText = commandText.Trim();

        try
        {
            var stopwatch = Stopwatch.StartNew();

#if(NETSTANDARD2_1)
            await using var command = connection.CreateCommand();
#else
            using var command = connection.CreateCommand();
#endif
            command.CommandText = commandText;
            await command.ExecuteNonQueryAsync();

            if (LocalDbLogging.SqlLoggingEnabled)
            {
                Trace.WriteLine($@"Executed SQL ({stopwatch.ElapsedMilliseconds}.ms):
{commandText.IndentLines()}", "LocalDB");
            }
        }
        catch (SqlException exception)
        {
            throw BuildException(connection, commandText, exception);
        }
        catch (Exception exception)
        {
            throw BuildException(connection, commandText, exception);
        }
    }

    static Exception BuildException(SqlConnection connection, string commandText, Exception exception, SqlErrorCollection? errors = null)
    {
        var builder = new StringBuilder($@"Failed to execute SQL command.
{nameof(commandText)}: {commandText}
connectionString: {connection.ConnectionString}
");
        AppendErrors(errors, builder);

        return new Exception(builder.ToString(), exception);
    }

    static void AppendErrors(SqlErrorCollection? errors, StringBuilder builder)
    {
        if (errors == null)
        {
            return;
        }

        builder.AppendLine("Errors:");
        foreach (SqlError sqlError in errors)
        {
            builder.AppendLine($" * ErrorNumber:{sqlError.Number}. LineNumber:{sqlError.LineNumber}. Message: {sqlError.Message}");
        }
    }
}