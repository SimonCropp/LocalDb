[TestFixture]
public class FormatOperationTests
{
    [Test]
    public Task CreateTable() =>
        Verify(PendingChanges.FormatOperation(
            new CreateTableOperation
            {
                Name = "Orders",
                Schema = "dbo",
                Columns =
                {
                    new() { Name = "Id", ClrType = typeof(int), ColumnType = "int", IsNullable = false },
                    new() { Name = "Name", ClrType = typeof(string), ColumnType = "nvarchar(200)", MaxLength = 200 }
                }
            }));

    [Test]
    public Task CreateTableNoSchema() =>
        Verify(PendingChanges.FormatOperation(
            new CreateTableOperation
            {
                Name = "Orders",
                Columns =
                {
                    new() { Name = "Id", ClrType = typeof(int), ColumnType = "int", IsNullable = false }
                }
            }));

    [Test]
    public Task DropTable() =>
        Verify(PendingChanges.FormatOperation(
            new DropTableOperation { Name = "Orders", Schema = "dbo" }));

    [Test]
    public Task DropTableNoSchema() =>
        Verify(PendingChanges.FormatOperation(
            new DropTableOperation { Name = "Orders" }));

    [Test]
    public Task RenameTable() =>
        Verify(PendingChanges.FormatOperation(
            new RenameTableOperation { Name = "Orders", Schema = "dbo", NewName = "PurchaseOrders" }));

    [Test]
    public Task RenameTableWithSchemaChange() =>
        Verify(PendingChanges.FormatOperation(
            new RenameTableOperation { Name = "Orders", Schema = "dbo", NewName = "Orders", NewSchema = "sales" }));

    [Test]
    public Task RenameTableNameOnly() =>
        Verify(PendingChanges.FormatOperation(
            new RenameTableOperation { Name = "Orders", NewName = "PurchaseOrders" }));

    [Test]
    public Task AlterTable() =>
        Verify(PendingChanges.FormatOperation(
            new AlterTableOperation { Name = "Orders", Schema = "dbo" }));

    [Test]
    public Task AddColumn() =>
        Verify(PendingChanges.FormatOperation(
            new AddColumnOperation
            {
                Table = "Orders",
                Schema = "dbo",
                Name = "Total",
                ClrType = typeof(decimal),
                ColumnType = "decimal(18,2)",
                IsNullable = false
            }));

    [Test]
    public Task AddColumnNullable() =>
        Verify(PendingChanges.FormatOperation(
            new AddColumnOperation
            {
                Table = "Orders",
                Name = "Notes",
                ClrType = typeof(string),
                ColumnType = "nvarchar(max)",
                IsNullable = true
            }));

    [Test]
    public Task AddColumnWithDefaultValue() =>
        Verify(PendingChanges.FormatOperation(
            new AddColumnOperation
            {
                Table = "Orders",
                Name = "Status",
                ClrType = typeof(int),
                ColumnType = "int",
                IsNullable = false,
                DefaultValue = 0
            }));

    [Test]
    public Task AddColumnWithDefaultValueSql() =>
        Verify(PendingChanges.FormatOperation(
            new AddColumnOperation
            {
                Table = "Orders",
                Name = "CreatedAt",
                ClrType = typeof(DateTime),
                ColumnType = "datetime2",
                IsNullable = false,
                DefaultValueSql = "GETUTCDATE()"
            }));

    [Test]
    public Task AddColumnWithComputedSql() =>
        Verify(PendingChanges.FormatOperation(
            new AddColumnOperation
            {
                Table = "Orders",
                Name = "FullName",
                ClrType = typeof(string),
                ColumnType = "nvarchar(max)",
                ComputedColumnSql = "[FirstName] + ' ' + [LastName]"
            }));

    [Test]
    public Task AddColumnWithMaxLength() =>
        Verify(PendingChanges.FormatOperation(
            new AddColumnOperation
            {
                Table = "Orders",
                Name = "Code",
                ClrType = typeof(string),
                ColumnType = "nvarchar(50)",
                MaxLength = 50,
                IsNullable = false
            }));

    [Test]
    public Task AddColumnFallbackClrType() =>
        Verify(PendingChanges.FormatOperation(
            new AddColumnOperation
            {
                Table = "Orders",
                Name = "Flag",
                ClrType = typeof(bool),
                IsNullable = false
            }));

    [Test]
    public Task DropColumn() =>
        Verify(PendingChanges.FormatOperation(
            new DropColumnOperation { Table = "Orders", Schema = "dbo", Name = "OldColumn" }));

    [Test]
    public Task AlterColumn() =>
        Verify(PendingChanges.FormatOperation(
            new AlterColumnOperation
            {
                Table = "Orders",
                Schema = "dbo",
                Name = "Total",
                ClrType = typeof(decimal),
                ColumnType = "decimal(18,4)",
                IsNullable = false
            }));

    [Test]
    public Task RenameColumn() =>
        Verify(PendingChanges.FormatOperation(
            new RenameColumnOperation { Table = "Orders", Schema = "dbo", Name = "OldName", NewName = "NewName" }));

    [Test]
    public Task CreateIndex() =>
        Verify(PendingChanges.FormatOperation(
            new CreateIndexOperation
            {
                Name = "IX_Orders_CustomerId",
                Table = "Orders",
                Schema = "dbo",
                Columns = ["CustomerId"]
            }));

    [Test]
    public Task CreateUniqueIndex() =>
        Verify(PendingChanges.FormatOperation(
            new CreateIndexOperation
            {
                Name = "IX_Orders_OrderNumber",
                Table = "Orders",
                Schema = "dbo",
                IsUnique = true,
                Columns = ["OrderNumber"]
            }));

    [Test]
    public Task CreateCompositeIndex() =>
        Verify(PendingChanges.FormatOperation(
            new CreateIndexOperation
            {
                Name = "IX_Orders_Customer_Date",
                Table = "Orders",
                Columns = ["CustomerId", "OrderDate"]
            }));

    [Test]
    public Task DropIndex() =>
        Verify(PendingChanges.FormatOperation(
            new DropIndexOperation { Name = "IX_Orders_CustomerId", Table = "Orders", Schema = "dbo" }));

    [Test]
    public Task RenameIndex() =>
        Verify(PendingChanges.FormatOperation(
            new RenameIndexOperation { Name = "IX_Old", Table = "Orders", Schema = "dbo", NewName = "IX_New" }));

    [Test]
    public Task AddForeignKey() =>
        Verify(PendingChanges.FormatOperation(
            new AddForeignKeyOperation
            {
                Name = "FK_Orders_Customers",
                Table = "Orders",
                Schema = "dbo",
                Columns = ["CustomerId"],
                PrincipalTable = "Customers",
                PrincipalSchema = "dbo",
                PrincipalColumns = ["Id"]
            }));

    [Test]
    public Task AddForeignKeyComposite() =>
        Verify(PendingChanges.FormatOperation(
            new AddForeignKeyOperation
            {
                Name = "FK_OrderItems_Orders",
                Table = "OrderItems",
                Columns = ["OrderId", "LineNumber"],
                PrincipalTable = "Orders",
                PrincipalColumns = ["Id", "LineNumber"]
            }));

    [Test]
    public Task DropForeignKey() =>
        Verify(PendingChanges.FormatOperation(
            new DropForeignKeyOperation { Name = "FK_Orders_Customers", Table = "Orders", Schema = "dbo" }));

    [Test]
    public Task AddPrimaryKey() =>
        Verify(PendingChanges.FormatOperation(
            new AddPrimaryKeyOperation
            {
                Name = "PK_Orders",
                Table = "Orders",
                Schema = "dbo",
                Columns = ["Id"]
            }));

    [Test]
    public Task AddCompositePrimaryKey() =>
        Verify(PendingChanges.FormatOperation(
            new AddPrimaryKeyOperation
            {
                Name = "PK_OrderItems",
                Table = "OrderItems",
                Columns = ["OrderId", "LineNumber"]
            }));

    [Test]
    public Task DropPrimaryKey() =>
        Verify(PendingChanges.FormatOperation(
            new DropPrimaryKeyOperation { Name = "PK_Orders", Table = "Orders", Schema = "dbo" }));

    [Test]
    public Task AddUniqueConstraint() =>
        Verify(PendingChanges.FormatOperation(
            new AddUniqueConstraintOperation
            {
                Name = "UQ_Orders_OrderNumber",
                Table = "Orders",
                Schema = "dbo",
                Columns = ["OrderNumber"]
            }));

    [Test]
    public Task DropUniqueConstraint() =>
        Verify(PendingChanges.FormatOperation(
            new DropUniqueConstraintOperation { Name = "UQ_Orders_OrderNumber", Table = "Orders", Schema = "dbo" }));

    [Test]
    public Task AddCheckConstraint() =>
        Verify(PendingChanges.FormatOperation(
            new AddCheckConstraintOperation { Name = "CK_Orders_Total", Table = "Orders", Schema = "dbo" }));

    [Test]
    public Task DropCheckConstraint() =>
        Verify(PendingChanges.FormatOperation(
            new DropCheckConstraintOperation { Name = "CK_Orders_Total", Table = "Orders", Schema = "dbo" }));

    [Test]
    public Task InsertData() =>
        Verify(PendingChanges.FormatOperation(
            new InsertDataOperation
            {
                Table = "Orders",
                Schema = "dbo",
                Columns = ["Id", "Name"],
                Values = new object[,] { { 1, "Test" } }
            }));

    [Test]
    public Task DeleteData() =>
        Verify(PendingChanges.FormatOperation(
            new DeleteDataOperation
            {
                Table = "Orders",
                Schema = "dbo",
                KeyColumns = ["Id"],
                KeyValues = new object[,] { { 1 } }
            }));

    [Test]
    public Task UpdateData() =>
        Verify(PendingChanges.FormatOperation(
            new UpdateDataOperation
            {
                Table = "Orders",
                Schema = "dbo",
                Columns = ["Name", "Total"],
                KeyColumns = ["Id"],
                Values = new object[,] { { "Updated", 100m } },
                KeyValues = new object[,] { { 1 } }
            }));

    [Test]
    public Task UnknownOperation() =>
        Verify(PendingChanges.FormatOperation(
            new SqlOperation { Sql = "SELECT 1" }));
}