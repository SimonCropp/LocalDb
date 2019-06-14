using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

static class SqlExtensions
{
    public static void ExecuteCommand(this SqlConnection connection, string commandText)
    {
        try
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandText;
                command.ExecuteNonQuery();
            }
        }
        catch (Exception exception)
        {
            throw new Exception(
                innerException: exception,
                message: $@"Failed to {nameof(ExecuteCommand)}
{nameof(commandText)}: {commandText}
connectionString: {connection.ConnectionString}
");
        }
    }
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
        catch (Exception exception)
        {
            throw new Exception(
                innerException: exception,
                message: $@"Failed to {nameof(ExecuteCommand)}
{nameof(commandText)}: {commandText}
connectionString: {connection.ConnectionString}
");
        }
    }
}