using EfLocalDb;

static class SuppliedTemplate
{
    static SqlInstance<MyDbContext> sqlInstance;

    static SuppliedTemplate()
    {
        sqlInstance = new SqlInstance<MyDbContext>(
            connection => new MyDbContext(connection),
            templatePath: "suppliedTemplate.mdf",
            logPath: "suppliedTemplate_log.ldf");
    }
}