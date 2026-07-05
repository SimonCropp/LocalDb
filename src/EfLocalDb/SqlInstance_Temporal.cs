namespace EfLocalDb;

public partial class SqlInstance<TDbContext>
    where TDbContext : DbContext
{
    // Temporal metadata is stripped from the runtime model, so reading it requires the
    // design-time model — a second full model compilation. Built lazily so instances that
    // never use the temporal APIs skip that cost at startup.
    Lazy<Dictionary<Type, TemporalSchema>> temporalSchemas;

    static Dictionary<Type, TemporalSchema> BuildTemporalSchemas(
        ConstructInstance<TDbContext> constructInstance,
        Action<SqlServerDbContextOptionsBuilder>? sqlOptionsBuilder)
    {
        var builder = DefaultOptionsBuilder.Build<TDbContext>();
        builder.UseSqlServer("Fake", sqlOptionsBuilder);
        using var context = constructInstance(builder);

        var schemas = new Dictionary<Type, TemporalSchema>();
        var designModel = context.GetService<IDesignTimeModel>().Model;
        foreach (var entityType in designModel.GetEntityTypes())
        {
            var schema = TemporalSchema.TryBuild(entityType);
            if (schema is not null)
            {
                schemas[entityType.ClrType] = schema;
            }
        }

        return schemas;
    }

    /// <summary>
    /// Re-stamps the entity's current PeriodStart and aligns the most recent history row's
    /// PeriodEnd to match. Use in tests to give consecutive saves distinct, deterministic
    /// temporal timestamps without Task.Delay. <paramref name="periodStart"/> must be greater
    /// than the row's previous PeriodStart or SQL Server rejects re-enabling system
    /// versioning due to overlapping periods.
    /// <para>The UPDATE bumps RowVersion. Caller is responsible for refreshing any tracked
    /// entity (or use the <see cref="SqlDatabase{TDbContext}.SetCurrentPeriodStart{TEntity}(TEntity, DateTime)"/>
    /// overload which reloads it).</para>
    /// </summary>
    public Task SetCurrentPeriodStart<TEntity>(TDbContext context, object id, DateTime periodStart)
        where TEntity : class =>
        ResolveSchema<TEntity>().Apply(context, id, periodStart);

    internal TemporalSchema ResolveSchema<TEntity>()
        where TEntity : class
    {
        if (temporalSchemas.Value.TryGetValue(typeof(TEntity), out var info))
        {
            return info;
        }

        throw new InvalidOperationException(
            $"{typeof(TEntity).Name} is not configured as a temporal table");
    }
}
