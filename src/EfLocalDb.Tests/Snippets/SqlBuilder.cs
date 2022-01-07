using EfLocalDb;
using Microsoft.EntityFrameworkCore;

public class SqlBuilder
{
    SqlBuilder()
    {
        #region sqlOptionsBuilder

        var sqlInstance = new SqlInstance<MyDbContext>(
            constructInstance: builder => new MyDbContext(builder.Options),
            sqlOptionsBuilder: sqlBuilder => sqlBuilder.EnableRetryOnFailure(5));

        #endregion
    }

    class MyDbContext:
        DbContext
    {
        public MyDbContext(DbContextOptions options) :
            base(options)
        {
        }
    }
}