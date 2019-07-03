using System.Linq;
using Microsoft.EntityFrameworkCore;

static class Untracker
{
    public static void DetachAllEntities(this DbContext context)
    {
        var changedEntriesCopy = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added ||
                        e.State == EntityState.Modified ||
                        e.State == EntityState.Deleted)
            .ToList();

        foreach (var entry in changedEntriesCopy)
        {
            entry.State = EntityState.Detached;
        }
    }
}