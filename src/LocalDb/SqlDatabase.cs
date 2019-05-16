using System.Data.SqlClient;
using System.Threading.Tasks;

namespace EFLocalDb
{
    public class SqlDatabase
    {
        public SqlDatabase(string connection)
        {
            Connection = connection;
        }

        public string Connection { get; }

        public async Task<SqlConnection> OpenConnection()
        {
            var sqlConnection = new SqlConnection(Connection);
            await sqlConnection.OpenAsync();
            return sqlConnection;
        }
    }
}