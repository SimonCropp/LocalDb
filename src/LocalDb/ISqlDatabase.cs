using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace LocalDb
{
    public interface ISqlDatabase :
#if(NETSTANDARD2_1)
        IAsyncDisposable,
#endif
        IDisposable
    {
        string ConnectionString { get; }
        string Name { get; }
        SqlConnection Connection { get; }
        Task<SqlConnection> OpenNewConnection();
        Task Start();
    }
}