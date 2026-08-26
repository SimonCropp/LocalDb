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
            }))
            .Snapshot("CreateTable: dbo.Orders Columns: [Id int NOT NULL, Name nvarchar(200) NOT NULL MaxLength=200]");

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
            }))
            .Snapshot("CreateTable: Orders Columns: [Id int NOT NULL]");

    [Test]
    public Task DropTable() =>
        Verify(PendingChanges.FormatOperation(
            new DropTableOperation { Name = "Orders", Schema = "dbo" }))
            .Snapshot("DropTable: dbo.Orders");

    [Test]
    public Task DropTableNoSchema() =>
        Verify(PendingChanges.FormatOperation(
            new DropTableOperation { Name = "Orders" }))
            .Snapshot("DropTable: Orders");

    [Test]
    public Task RenameTable() =>
        Verify(PendingChanges.FormatOperation(
            new RenameTableOperation { Name = "Orders", Schema = "dbo", NewName = "PurchaseOrders" }))
            .Snapshot("RenameTable: dbo.Orders -> PurchaseOrders");

    [Test]
    public Task RenameTableWithSchemaChange() =>
        Verify(PendingChanges.FormatOperation(
            new RenameTableOperation { Name = "Orders", Schema = "dbo", NewName = "Orders", NewSchema = "sales" }))
            .Snapshot("RenameTable: dbo.Orders -> sales.Orders");

    [Test]
    public Task RenameTableNameOnly() =>
        Verify(PendingChanges.FormatOperation(
            new RenameTableOperation { Name = "Orders", NewName = "PurchaseOrders" }))
            .Snapshot("RenameTable: Orders -> PurchaseOrders");

    [Test]
    public Task AlterTable() =>
        Verify(PendingChanges.FormatOperation(
            new AlterTableOperation { Name = "Orders", Schema = "dbo" }))
            .Snapshot("AlterTable: dbo.Orders");

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
            }))
            .Snapshot("AddColumn: dbo.Orders.Total decimal(18,2) NOT NULL");

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
            }))
            .Snapshot("AddColumn: Orders.Notes nvarchar(max)");

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
            }))
            .Snapshot("AddColumn: Orders.Status int NOT NULL DEFAULT 0");

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
            }))
            .Snapshot("AddColumn: Orders.CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE()");

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
            }))
            .Snapshot("AddColumn: Orders.FullName nvarchar(max) NOT NULL AS [FirstName] + ' ' + [LastName]");

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
            }))
            .Snapshot("AddColumn: Orders.Code nvarchar(50) NOT NULL MaxLength=50");

    [Test]
    public Task AddColumnFallbackClrType() =>
        Verify(PendingChanges.FormatOperation(
            new AddColumnOperation
            {
                Table = "Orders",
                Name = "Flag",
                ClrType = typeof(bool),
                IsNullable = false
            }))
            .Snapshot("AddColumn: Orders.Flag Boolean NOT NULL");

    [Test]
    public Task DropColumn() =>
        Verify(PendingChanges.FormatOperation(
            new DropColumnOperation { Table = "Orders", Schema = "dbo", Name = "OldColumn" }))
            .Snapshot("DropColumn: dbo.Orders.OldColumn");

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
            }))
            .Snapshot("AlterColumn: dbo.Orders.Total decimal(18,4) NOT NULL");

    [Test]
    public Task RenameColumn() =>
        Verify(PendingChanges.FormatOperation(
            new RenameColumnOperation { Table = "Orders", Schema = "dbo", Name = "OldName", NewName = "NewName" }))
            .Snapshot("RenameColumn: dbo.Orders.OldName -> NewName");

    [Test]
    public Task CreateIndex() =>
        Verify(PendingChanges.FormatOperation(
            new CreateIndexOperation
            {
                Name = "IX_Orders_CustomerId",
                Table = "Orders",
                Schema = "dbo",
                Columns = ["CustomerId"]
            }))
            .Snapshot("CreateIndex: IX_Orders_CustomerId on dbo.Orders [CustomerId]");

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
            }))
            .Snapshot("CreateIndex: unique IX_Orders_OrderNumber on dbo.Orders [OrderNumber]");

    [Test]
    public Task CreateCompositeIndex() =>
        Verify(PendingChanges.FormatOperation(
            new CreateIndexOperation
            {
                Name = "IX_Orders_Customer_Date",
                Table = "Orders",
                Columns = ["CustomerId", "OrderDate"]
            }))
            .Snapshot("CreateIndex: IX_Orders_Customer_Date on Orders [CustomerId, OrderDate]");

    [Test]
    public Task DropIndex() =>
        Verify(PendingChanges.FormatOperation(
            new DropIndexOperation { Name = "IX_Orders_CustomerId", Table = "Orders", Schema = "dbo" }))
            .Snapshot("DropIndex: IX_Orders_CustomerId on dbo.Orders");

    [Test]
    public Task RenameIndex() =>
        Verify(PendingChanges.FormatOperation(
            new RenameIndexOperation { Name = "IX_Old", Table = "Orders", Schema = "dbo", NewName = "IX_New" }))
            .Snapshot("RenameIndex: dbo.Orders IX_Old -> IX_New");

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
            }))
            .Snapshot("AddForeignKey: FK_Orders_Customers on dbo.Orders [CustomerId] -> dbo.Customers [Id]");

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
            }))
            .Snapshot("AddForeignKey: FK_OrderItems_Orders on OrderItems [OrderId, LineNumber] -> Orders [Id, LineNumber]");

    [Test]
    public Task DropForeignKey() =>
        Verify(PendingChanges.FormatOperation(
            new DropForeignKeyOperation { Name = "FK_Orders_Customers", Table = "Orders", Schema = "dbo" }))
            .Snapshot("DropForeignKey: FK_Orders_Customers on dbo.Orders");

    [Test]
    public Task AddPrimaryKey() =>
        Verify(PendingChanges.FormatOperation(
            new AddPrimaryKeyOperation
            {
                Name = "PK_Orders",
                Table = "Orders",
                Schema = "dbo",
                Columns = ["Id"]
            }))
            .Snapshot("AddPrimaryKey: PK_Orders on dbo.Orders [Id]");

    [Test]
    public Task AddCompositePrimaryKey() =>
        Verify(PendingChanges.FormatOperation(
            new AddPrimaryKeyOperation
            {
                Name = "PK_OrderItems",
                Table = "OrderItems",
                Columns = ["OrderId", "LineNumber"]
            }))
            .Snapshot("AddPrimaryKey: PK_OrderItems on OrderItems [OrderId, LineNumber]");

    [Test]
    public Task DropPrimaryKey() =>
        Verify(PendingChanges.FormatOperation(
            new DropPrimaryKeyOperation { Name = "PK_Orders", Table = "Orders", Schema = "dbo" }))
            .Snapshot("DropPrimaryKey: PK_Orders on dbo.Orders");

    [Test]
    public Task AddUniqueConstraint() =>
        Verify(PendingChanges.FormatOperation(
            new AddUniqueConstraintOperation
            {
                Name = "UQ_Orders_OrderNumber",
                Table = "Orders",
                Schema = "dbo",
                Columns = ["OrderNumber"]
            }))
            .Snapshot("AddUniqueConstraint: UQ_Orders_OrderNumber on dbo.Orders [OrderNumber]");

    [Test]
    public Task DropUniqueConstraint() =>
        Verify(PendingChanges.FormatOperation(
            new DropUniqueConstraintOperation { Name = "UQ_Orders_OrderNumber", Table = "Orders", Schema = "dbo" }))
            .Snapshot("DropUniqueConstraint: UQ_Orders_OrderNumber on dbo.Orders");

    [Test]
    public Task AddCheckConstraint() =>
        Verify(PendingChanges.FormatOperation(
            new AddCheckConstraintOperation { Name = "CK_Orders_Total", Table = "Orders", Schema = "dbo" }))
            .Snapshot("AddCheckConstraint: CK_Orders_Total on dbo.Orders");

    [Test]
    public Task DropCheckConstraint() =>
        Verify(PendingChanges.FormatOperation(
            new DropCheckConstraintOperation { Name = "CK_Orders_Total", Table = "Orders", Schema = "dbo" }))
            .Snapshot("DropCheckConstraint: CK_Orders_Total on dbo.Orders");

    [Test]
    public Task InsertData() =>
        Verify(PendingChanges.FormatOperation(
            new InsertDataOperation
            {
                Table = "Orders",
                Schema = "dbo",
                Columns = ["Id", "Name"],
                Values = new object[,] { { 1, "Test" } }
            }))
            .Snapshot("InsertData: dbo.Orders [Id, Name]");

    [Test]
    public Task DeleteData() =>
        Verify(PendingChanges.FormatOperation(
            new DeleteDataOperation
            {
                Table = "Orders",
                Schema = "dbo",
                KeyColumns = ["Id"],
                KeyValues = new object[,] { { 1 } }
            }))
            .Snapshot("DeleteData: dbo.Orders");

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
            }))
            .Snapshot("UpdateData: dbo.Orders [Name, Total]");

    [Test]
    public Task UnknownOperation() =>
        Verify(PendingChanges.FormatOperation(
            new SqlOperation { Sql = "SELECT 1" }))
            .Snapshot("Sql");
}