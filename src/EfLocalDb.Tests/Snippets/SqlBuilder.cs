using EfLocalDb;
using Microsoft.EntityFrameworkCore;

public class SqlBuilder
{
    SqlBuilder()
    {
        #region sqlOptionsBuilder

        SqlInstance<MyDbContext> sqlInstance = new(
            constructInstance: builder => new(builder.Options),
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