﻿using Microsoft.EntityFrameworkCore;

public class MyDbContext :
    DbContext
{
    public DbSet<TheEntity> TestEntities { get; set; } = null!;

    public MyDbContext(DbContextOptions options) :
        base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder model) => model.Entity<TheEntity>();
}