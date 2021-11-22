using Microsoft.Data.SqlClient;

static class SqlExtensions
{
    public static async Task ExecuteCommandAsync(this SqlConnection connection, string commandText)
    {
        commandText = commandText.Trim();

        try
        {
            var stopwatch = Stopwatch.StartNew();

            await using (var command = connection.CreateCommand())
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
        catch (SqlException exception)
        {
            throw BuildException(connection, commandText, exception);
        }
        catch (Exception exception)
        {
            throw BuildException(connection, commandText, exception);
        }
    }

    static Exception BuildException(SqlConnection connection, string commandText, Exception exception)
    {
        StringBuilder builder = new($@"Failed to execute SQL command.
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