using System;
using System.Data.Entity;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace EfLocalDb
{
    public interface ISqlDatabase<out TDbContext>:
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