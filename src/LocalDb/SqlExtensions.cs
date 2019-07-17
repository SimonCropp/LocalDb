using System;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;

static class SqlExtensions
{
    public static async Task ExecuteCommandAsync(this SqlConnection connection, string commandText)
    {
        try
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;
                await command.ExecuteNonQueryAsync();
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

    static Exception BuildException(SqlConnection connection, string commandText, Exception exception, SqlErrorCollection errors = null)
    {
        var builder = new StringBuilder($@"Failed to execute SQL command.
{nameof(commandText)}: {commandText}
connectionString: {connection.ConnectionString}
");
        AppendErrors(errors, builder);

        return new Exception(builder.ToString(), exception);
    }

    static void AppendErrors(SqlErrorCollection errors, StringBuilder builder)
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