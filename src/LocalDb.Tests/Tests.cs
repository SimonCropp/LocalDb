using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using EFLocalDb;
using Xunit;
using Xunit.Abstractions;

public class Tests:
    XunitLoggingBase
{
    [Fact]
    public async Task Simple()
    {
        var instance = new SqlInstance(buildTemplate: CreateTable, name: "Name");

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
    public async Task WithRebuild()
    {
        var instance1 = new SqlInstance(
            buildTemplate: CreateTable,
            requiresRebuild: dbContext => true, 
            name: "rebuild");
        var database1 = await instance1.Build();
        using (var connection = await database1.OpenConnection())
        {
            await AddData(connection);
        }

        var instance2 = new SqlInstance(
            buildTemplate: (connection) => throw new Exception(),
            requiresRebuild: dbContext => false,
            name: "rebuild");
        var database2 = await instance2.Build();
        using (var connection = await database2.OpenConnection())
        {
            await AddData(connection);
        }
        
        using (var connection = await database2.OpenConnection())
        {
            var data = await GetData(connection);
            Assert.Equal(2, data.Count);
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
            command.CommandText = @"
select Value from MyTable";
            var reader = await command.ExecuteReaderAsync();
            while (reader.Read())
            {
                values.Add(reader.GetInt32(0));
            }
        }
        return values;
    }
}