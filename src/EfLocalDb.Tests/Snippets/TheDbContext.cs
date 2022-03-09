﻿using Microsoft.EntityFrameworkCore;

public class TheDbContext :
    DbContext
{
    public DbSet<TheEntity> TestEntities { get; set; } = null!;

    public TheDbContext(DbContextOptions options) :
        base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder model) => model.Entity<TheEntity>();
}