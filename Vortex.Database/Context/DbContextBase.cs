using Microsoft.EntityFrameworkCore;
using Vortex.Database.Extensions;

namespace Vortex.Database.Context;

public abstract class DbContextBase<TContent>(DbContextOptions<TContent> options)
    : DbContext(options)
    where TContent : DbContext
{
    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyDefaultAttributesFromEntities();
        mb.ApplyConventions();
        mb.ApplySoftDeleteQueryFilter();
    }
}
