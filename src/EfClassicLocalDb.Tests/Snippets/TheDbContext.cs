﻿using System.Data.Entity;
using Microsoft.Data.SqlClient;

public class TheDbContext :
    DbContext
{
    public DbSet<TheEntity> TestEntities { get; set; } = null!;

    public TheDbContext(SqlConnection connection) :
        base(connection, false)
    {
    }

    protected override void OnModelCreating(DbModelBuilder model)
    {
        model.Entity<TheEntity>();
    }
}