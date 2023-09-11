using EfLocalDb;
using Microsoft.EntityFrameworkCore;
// ReSharper disable UnusedVariable

public class SqlBuilder
{
    SqlBuilder()
    {
        #region sqlOptionsBuilder

        var sqlInstance = new SqlInstance<MyDbContext>(
            constructInstance: builder => new(builder.Options),
            sqlOptionsBuilder: sqlBuilder => sqlBuilder.EnableRetryOnFailure(5));

        #endregion
    }

    class MyDbContext(DbContextOptions options) :
        DbContext(options);
}