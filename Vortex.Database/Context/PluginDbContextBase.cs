using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Vortex.Database.Delegates;
using Vortex.Database.Extensions;

namespace Vortex.Database.Context;

public class PluginDbContextBase<TContent>(
    DbContextOptions<TContent> options,
    TablePrefixProvider prefix
) : DbContextBase<TContent>(options)
    where TContent : DbContext
{
    private readonly TablePrefixProvider _prefix = prefix;

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        foreach (
            IMutableForeignKey fk in mb.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys())
        )
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }

        mb.ApplyTablePrefix(_prefix());
    }
}
