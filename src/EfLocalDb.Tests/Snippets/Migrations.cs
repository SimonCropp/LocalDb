﻿// ReSharper disable UnusedParameter.Local

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
            constructInstance: builder => new(builder.Options));

        #endregion
    }

    class MigrationsGenerator :
        IMigrationsSqlGenerator
    {
        public IReadOnlyList<MigrationCommand> Generate(
            IReadOnlyList<MigrationOperation> operations,
            IModel? model = null,
            MigrationsSqlGenerationOptions options = MigrationsSqlGenerationOptions.Default) =>
            throw new();
    }

    class MyDbContext :
        DbContext
    {
        public DbSet<TheEntity> TestEntities { get; set; } = null!;

        public MyDbContext(DbContextOptions options) :
            base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder model) => model.Entity<TheEntity>();
    }
}