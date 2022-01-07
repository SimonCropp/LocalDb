using EfLocalDb;

static class SuppliedTemplate
{
    static SqlInstance<MyDbContext> sqlInstance;

    static SuppliedTemplate()
    {
        sqlInstance = new SqlInstance<MyDbContext>(
            connection => new MyDbContext(connection),
            existingTemplate: new ExistingTemplate("template.mdf", "template_log.ldf"));
    }
}