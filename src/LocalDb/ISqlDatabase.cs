using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace LocalDb
{
    public interface ISqlDatabase :
        IAsyncDisposable,
        IDisposable
    {
        string ConnectionString { get; }
        string Name { get; }
        SqlConnection Connection { get; }
        Task<SqlConnection> OpenNewConnection();
        Task Start();
    }
}