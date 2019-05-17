using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using ApprovalTests;
using LocalDb;
using Xunit;
using Xunit.Abstractions;

public class Tests:
    XunitLoggingBase
{
    [Fact]
    public async Task Simple()
    {
        var instance = new SqlInstance(name: "Name", buildTemplate: CreateTable);

        var localDb = await instance.Build();
        using (var connection = await localDb.OpenConnection())
        {
            await AddData(connection);
        }

        using (var connection = await localDb.OpenConnection())
        {
            Assert.Single(await GetData(connection));
        }
    }
    [Fact]
    public void DuplicateDbContext()
    {
        Register();
        var exception = Assert.Throws<Exception>(Register);
        Approvals.Verify(exception.Message);
    }

    static void Register()
    {
        LocalDb.SqlInstanceService.Register("LocalDbDuplicateDbContext", CreateTable);
    }

    [Fact]
    public async Task WithRebuild()
    {
        var instance1 = new SqlInstance(name: "rebuild", buildTemplate: CreateTable, requiresRebuild: dbContext => true);
        var database1 = await instance1.Build();
        using (var connection = await database1.OpenConnection())
        {
            await AddData(connection);
        }

        var instance2 = new SqlInstance(name: "rebuild", buildTemplate: connection => throw new Exception(), requiresRebuild: dbContext => false);
        var database2 = await instance2.Build();
        using (var connection = await database2.OpenConnection())
        {
            await AddData(connection);
        }

        using (var connection = await database2.OpenConnection())
        {
            var data = await GetData(connection);
            Assert.Single(data);
        }
    }

    public Tests(ITestOutputHelper output) :
        base(output)
    {
    }

    static void CreateTable(SqlConnection connection)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "create table MyTable (Value int);";
            command.ExecuteNonQuery();
        }
    }

    static async Task AddData(SqlConnection connection)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = @"
insert into MyTable (Value)
values (1);";
            await command.ExecuteNonQueryAsync();
        }
    }

    static async Task<List<int>> GetData(SqlConnection connection)
    {
        var values = new List<int>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "select Value from MyTable";
            var reader = await command.ExecuteReaderAsync();
            while (reader.Read())
            {
                values.Add(reader.GetInt32(0));
            }
        }
        return values;
    }
}