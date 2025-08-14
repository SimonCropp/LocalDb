namespace EfLocalDbNunit;

public class QueryableSettingsTask<T>:SettingsTask
{
    public QueryableSettingsTask(VerifySettings? settings, Func<VerifySettings, Task<VerifyResult>> buildTask) :
        base(settings, buildTask)
    {
    }
}