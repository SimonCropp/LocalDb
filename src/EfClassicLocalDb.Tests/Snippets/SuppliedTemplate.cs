using EfLocalDb;

static class SuppliedTemplate
{
    static SqlInstance<MyDbContext> sqlInstance;

    static SuppliedTemplate()
    {
        sqlInstance = new(
            connection => new(connection),
            existingTemplate: new("template.mdf", "template_log.ldf"));
    }
}