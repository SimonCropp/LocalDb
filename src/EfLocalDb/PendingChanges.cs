public static class PendingChanges
{
    public static void ThrowIfPendingChanges<TDbContext>(this TDbContext data) where TDbContext : DbContext
    {
        var pendingChanges = GetPendingChanges(data);
        if (pendingChanges.Count > 0)
        {
            throw new InvalidOperationException(PendingChangesMessage<TDbContext>(pendingChanges));
        }
    }

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

    internal static string FormatOperation(MigrationOperation operation) =>
        operation switch
        {
            CreateTableOperation op => $"CreateTable: {QualifiedTable(op.Schema, op.Name)} Columns: [{string.Join(", ", op.Columns.Select(c => $"{c.Name} {FormatColumnType(c)}"))}]",
            DropTableOperation op => $"DropTable: {QualifiedTable(op.Schema, op.Name)}",
            RenameTableOperation op => $"RenameTable: {QualifiedTable(op.Schema, op.Name)} -> {QualifiedTable(op.NewSchema, op.NewName ?? op.Name)}",
            AlterTableOperation op => $"AlterTable: {QualifiedTable(op.Schema, op.Name)}",
            AddColumnOperation op => $"AddColumn: {QualifiedTable(op.Schema, op.Table)}.{op.Name} {FormatColumnType(op)}",
            DropColumnOperation op => $"DropColumn: {QualifiedTable(op.Schema, op.Table)}.{op.Name}",
            AlterColumnOperation op => $"AlterColumn: {QualifiedTable(op.Schema, op.Table)}.{op.Name} {FormatColumnType(op)}",
            RenameColumnOperation op => $"RenameColumn: {QualifiedTable(op.Schema, op.Table)}.{op.Name} -> {op.NewName}",
            CreateIndexOperation op => $"CreateIndex: {(op.IsUnique ? "unique " : "")}{op.Name} on {QualifiedTable(op.Schema, op.Table)} [{string.Join(", ", op.Columns)}]",
            DropIndexOperation op => $"DropIndex: {op.Name} on {QualifiedTable(op.Schema, op.Table!)}",
            RenameIndexOperation op => $"RenameIndex: {QualifiedTable(op.Schema, op.Table!)} {op.Name} -> {op.NewName}",
            AddForeignKeyOperation op => $"AddForeignKey: {op.Name} on {QualifiedTable(op.Schema, op.Table)} [{string.Join(", ", op.Columns)}] -> {QualifiedTable(op.PrincipalSchema, op.PrincipalTable)} [{string.Join(", ", op.PrincipalColumns ?? [])}]",
            DropForeignKeyOperation op => $"DropForeignKey: {op.Name} on {QualifiedTable(op.Schema, op.Table)}",
            AddPrimaryKeyOperation op => $"AddPrimaryKey: {op.Name} on {QualifiedTable(op.Schema, op.Table)} [{string.Join(", ", op.Columns)}]",
            DropPrimaryKeyOperation op => $"DropPrimaryKey: {op.Name} on {QualifiedTable(op.Schema, op.Table)}",
            AddUniqueConstraintOperation op => $"AddUniqueConstraint: {op.Name} on {QualifiedTable(op.Schema, op.Table)} [{string.Join(", ", op.Columns)}]",
            DropUniqueConstraintOperation op => $"DropUniqueConstraint: {op.Name} on {QualifiedTable(op.Schema, op.Table)}",
            AddCheckConstraintOperation op => $"AddCheckConstraint: {op.Name} on {QualifiedTable(op.Schema, op.Table)}",
            DropCheckConstraintOperation op => $"DropCheckConstraint: {op.Name} on {QualifiedTable(op.Schema, op.Table)}",
            InsertDataOperation op => $"InsertData: {QualifiedTable(op.Schema, op.Table)} [{string.Join(", ", op.Columns)}]",
            DeleteDataOperation op => $"DeleteData: {QualifiedTable(op.Schema, op.Table)}",
            UpdateDataOperation op => $"UpdateData: {QualifiedTable(op.Schema, op.Table)} [{string.Join(", ", op.Columns)}]",
            _ => operation.GetType().Name.Replace("Operation", "")
        };

    static string FormatColumnType(ColumnOperation column)
    {
        var type = column.ColumnType ?? column.ClrType.Name;
        if (!column.IsNullable)
        {
            type += " NOT NULL";
        }

        if (column.DefaultValueSql is not null)
        {
            type += $" DEFAULT {column.DefaultValueSql}";
        }
        else if (column.DefaultValue is not null)
        {
            type += $" DEFAULT {column.DefaultValue}";
        }

        if (column.ComputedColumnSql is not null)
        {
            type += $" AS {column.ComputedColumnSql}";
        }

        if (column.MaxLength is not null)
        {
            type += $" MaxLength={column.MaxLength}";
        }

        return type;
    }

    static string QualifiedTable(string? schema, string name) =>
        schema is null ? name : $"{schema}.{name}";
}
