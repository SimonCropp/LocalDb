using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

static class BuildTemplateConverter
{
    public static TemplateFromConnection<TDbContext> Convert<TDbContext>(
        ConstructInstance<TDbContext> constructInstance,
        TemplateFromContext<TDbContext>? buildTemplate)
        where TDbContext : DbContext =>
        async (_, builder) =>
        {
            await using var data = constructInstance(builder);
            if (buildTemplate is null)
            {
                await data.Database.EnsureCreatedAsync();
            }
            else
            {
                var pendingChanges = GetPendingChanges(data);
                if (pendingChanges.Count > 0)
                {
                    throw new InvalidOperationException(PendingChangesMessage<TDbContext>(pendingChanges));
                }

                await buildTemplate(data);
            }
        };

    static IReadOnlyList<MigrationOperation> GetPendingChanges(DbContext data)
    {
        var serviceProvider = ((IInfrastructure<IServiceProvider>)data).Instance;
        var differ = serviceProvider.GetRequiredService<IMigrationsModelDiffer>();
        var migrationsAssembly = serviceProvider.GetRequiredService<IMigrationsAssembly>();
        var designTimeModel = serviceProvider.GetRequiredService<IDesignTimeModel>();
        var modelInitializer = serviceProvider.GetRequiredService<IModelRuntimeInitializer>();

        var snapshotModel = migrationsAssembly.ModelSnapshot?.Model;

        if (snapshotModel is null)
        {
            return [];
        }

        if (snapshotModel is IMutableModel mutableModel)
        {
            snapshotModel = mutableModel.FinalizeModel();
        }

        snapshotModel = modelInitializer.Initialize(snapshotModel);

        return differ.GetDifferences(
            snapshotModel.GetRelationalModel(),
            designTimeModel.Model.GetRelationalModel());
    }

    static string PendingChangesMessage<TDbContext>(IReadOnlyList<MigrationOperation> differences)
    {
        var builder = new StringBuilder(
            $"The model for context '{typeof(TDbContext).Name}' has pending changes. Add a new migration before updating the database.");
        builder.AppendLine();
        builder.AppendLine("Pending changes:");

        foreach (var op in differences)
        {
            builder.Append("  ");
            builder.AppendLine(FormatOperation(op));
        }

        return builder.ToString();
    }

    static string FormatOperation(MigrationOperation operation) =>
        operation switch
        {
            CreateTableOperation op => $"CreateTable: {QualifiedName(op.Schema, op.Name)}",
            DropTableOperation op => $"DropTable: {QualifiedName(op.Schema, op.Name)}",
            RenameTableOperation op => $"RenameTable: {QualifiedName(op.Schema, op.Name)} -> {op.NewName}",
            AddColumnOperation op => $"AddColumn: {QualifiedName(op.Schema, op.Table)}.{op.Name} ({op.ClrType.Name})",
            DropColumnOperation op => $"DropColumn: {QualifiedName(op.Schema, op.Table)}.{op.Name}",
            AlterColumnOperation op => $"AlterColumn: {QualifiedName(op.Schema, op.Table)}.{op.Name}",
            RenameColumnOperation op => $"RenameColumn: {QualifiedName(op.Schema, op.Table)}.{op.Name} -> {op.NewName}",
            CreateIndexOperation op => $"CreateIndex: {op.Name} on {QualifiedName(op.Schema, op.Table)}",
            DropIndexOperation op => $"DropIndex: {op.Name} on {QualifiedName(op.Schema, op.Table!)}",
            AddForeignKeyOperation op => $"AddForeignKey: {op.Name} on {QualifiedName(op.Schema, op.Table)}",
            DropForeignKeyOperation op => $"DropForeignKey: {op.Name} on {QualifiedName(op.Schema, op.Table)}",
            _ => operation.GetType().Name.Replace("Operation", "")
        };

    static string QualifiedName(string? schema, string name) =>
        schema is null ? name : $"{schema}.{name}";
}
