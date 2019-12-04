using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EfLocalDb
{
    public interface ISqlDatabase<out TDbContext>:
#if(NETSTANDARD2_1)
        IAsyncDisposable,
#endif
        IDisposable
        where TDbContext : DbContext
    {
        string Name { get; }
        SqlConnection Connection { get; }
        string ConnectionString { get; }
        TDbContext Context { get; }
        Task<SqlConnection> OpenNewConnection();
        Task Start();
        TDbContext NewDbContext();
    }
}