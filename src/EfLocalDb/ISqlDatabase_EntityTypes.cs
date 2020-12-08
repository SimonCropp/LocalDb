using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EfLocalDb
{
    public partial interface ISqlDatabase<out TDbContext>
    {
        IReadOnlyList<IEntityType> EntityTypes { get; }
    }
}