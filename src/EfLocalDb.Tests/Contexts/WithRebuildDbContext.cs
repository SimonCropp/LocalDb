﻿using Microsoft.EntityFrameworkCore;

public class WithRebuildDbContext(DbContextOptions options) :
    DbContext(options)
{
    public DbSet<TestEntity> TestEntities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder model) => model.Entity<TestEntity>();
}