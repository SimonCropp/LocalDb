﻿using System.Data.Common;
#if !EF
using Microsoft.Data.SqlClient;
#endif

static class SqlExtensions
{
    public static async Task ExecuteCommandAsync(this DbConnection connection, string commandText)
    {
        commandText = commandText.Trim();

        try
        {
            var stopwatch = Stopwatch.StartNew();

#if(NET5_0_OR_GREATER)
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
#if !EF
        if (exception is SqlException sqlException)
        {
            builder.AppendLine("SqlErrors:");
            foreach (SqlError error in sqlException.Errors)
            {
                builder.AppendLine($"    {error.Message}");
            }
        }
#endif
        return new(builder.ToString(), exception);
    }
}