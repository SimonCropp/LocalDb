using Microsoft.EntityFrameworkCore.Query;

static class FilterToggle
{
    public static void DisableQueryFilters(this DbContext context)
    {
        var factory = (FilteredCompilationContextFactory) context.GetService<IQueryCompilationContextFactory>();
        factory.FilterFlag.Value = true;
    }

    public static void EnableQueryFilters(this DbContext context)
    {
        var factory = (FilteredCompilationContextFactory) context.GetService<IQueryCompilationContextFactory>();
        factory.FilterFlag.Value = false;
    }
}

