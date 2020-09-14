using System;
using System.Collections.Generic;
using EfLocalDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

public class Migrations
{
    Migrations()
    {
        #region Migrations

        var sqlInstance = new SqlInstance<MyDbContext>(
            buildTemplate: async (connection, options) =>
            {
                #region IMigrationsSqlGenerator
                options.ReplaceService<IMigrationsSqlGenerator, MigrationsGenerator>();
                #endregion
                #region Migrate

                await using var data = new MyDbContext(options.Options);
                await data.Database.MigrateAsync();
                #endregion
            },
            constructInstance: builder => new MyDbContext(builder.Options));

        #endregion
    }

    class MigrationsGenerator:
        IMigrationsSqlGenerator
    {
        public IReadOnlyList<MigrationCommand> Generate(
            IReadOnlyList<MigrationOperation> operations,
            IModel? model = null,
            MigrationsSqlGenerationOptions options = MigrationsSqlGenerationOptions.Default)
        {
            throw new NotImplementedException();
        }
    }

    class MyDbContext:
        DbContext
    {
        public DbSet<TheEntity> TestEntities { get; set; } = null!;

        public MyDbContext(DbContextOptions options) :
            base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder model)
        {
            model.Entity<TheEntity>();
        }
    }
}