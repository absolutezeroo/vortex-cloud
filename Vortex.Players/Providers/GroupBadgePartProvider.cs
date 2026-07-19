using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Groups;
using Vortex.Primitives.Groups.Providers;
using Vortex.Primitives.Groups.Snapshots;

namespace Vortex.Players.Providers;

public sealed class GroupBadgePartProvider(
    IDbContextFactory<TurboDbContext> dbCtxFactory,
    ILogger<IGroupBadgePartProvider> logger
) : IGroupBadgePartProvider
{
    private readonly IDbContextFactory<TurboDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<IGroupBadgePartProvider> _logger = logger;

    private ImmutableArray<GroupBadgePartOptionSnapshot> _baseParts = [];
    private ImmutableArray<GroupBadgePartOptionSnapshot> _layerParts = [];
    private ImmutableArray<GroupColorOptionSnapshot> _colors = [];

    public IReadOnlyList<GroupBadgePartOptionSnapshot> BaseParts => _baseParts;

    public IReadOnlyList<GroupBadgePartOptionSnapshot> LayerParts => _layerParts;

    public IReadOnlyList<GroupColorOptionSnapshot> Colors => _colors;

    public string ResolveColorHex(string? colorId)
    {
        ImmutableArray<GroupColorOptionSnapshot> colors = _colors;

        if (int.TryParse(colorId, out int id))
        {
            GroupColorOptionSnapshot? match = colors.FirstOrDefault(c => c.Id == id);
            if (match is not null)
            {
                return match.ColorHex;
            }
        }

        return colors.Length > 0 ? colors[0].ColorHex : "ffffff";
    }

    public async Task ReloadAsync(CancellationToken ct)
    {
        TurboDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        try
        {
            List<GroupBadgePartEntity> parts = await dbCtx
                .GroupBadgeParts.AsNoTracking()
                .Where(p => p.Enabled)
                .OrderBy(p => p.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            ImmutableArray<GroupBadgePartOptionSnapshot> baseParts = parts
                .Where(p => p.Type == "base")
                .Select(p => new GroupBadgePartOptionSnapshot
                {
                    Id = p.PartId,
                    FileName = p.FileName,
                    MaskFileName = p.MaskFileName,
                })
                .ToImmutableArray();

            ImmutableArray<GroupBadgePartOptionSnapshot> layerParts = parts
                .Where(p => p.Type == "symbol")
                .Select(p => new GroupBadgePartOptionSnapshot
                {
                    Id = p.PartId,
                    FileName = p.FileName,
                    MaskFileName = p.MaskFileName,
                })
                .ToImmutableArray();

            List<GroupColorEntity> colors = await dbCtx
                .GroupColors.AsNoTracking()
                .OrderBy(c => c.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            ImmutableArray<GroupColorOptionSnapshot> colorOptions = colors
                .Select(c => new GroupColorOptionSnapshot { Id = c.ColorId, ColorHex = c.ColorHex })
                .ToImmutableArray();

            _baseParts = baseParts;
            _layerParts = layerParts;
            _colors = colorOptions;

            _logger.LogInformation(
                "Loaded group badge parts: {Bases} bases, {Symbols} symbols, {Colors} colors",
                baseParts.Length,
                layerParts.Length,
                colorOptions.Length
            );
        }
        finally
        {
            await dbCtx.DisposeAsync().ConfigureAwait(false);
        }
    }
}
