using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Turbo.Database.Context;
using Turbo.Database.Entities.Groups;
using Turbo.Primitives.Groups.Providers;
using Turbo.Primitives.Groups.Snapshots;

namespace Turbo.Players.Providers;

public sealed class GroupBadgePartProvider(
    IDbContextFactory<TurboDbContext> dbCtxFactory,
    ILogger<IGroupBadgePartProvider> logger
) : IGroupBadgePartProvider
{
    private readonly IDbContextFactory<TurboDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<IGroupBadgePartProvider> _logger = logger;

    private List<GroupBadgePartOptionSnapshot> _baseParts = [];
    private List<GroupBadgePartOptionSnapshot> _layerParts = [];
    private List<GroupColorOptionSnapshot> _colors = [];

    public IReadOnlyList<GroupBadgePartOptionSnapshot> BaseParts => _baseParts;

    public IReadOnlyList<GroupBadgePartOptionSnapshot> LayerParts => _layerParts;

    public IReadOnlyList<GroupColorOptionSnapshot> Colors => _colors;

    public string ResolveColorHex(string? colorId)
    {
        if (int.TryParse(colorId, out int id))
        {
            GroupColorOptionSnapshot? match = _colors.FirstOrDefault(c => c.Id == id);
            if (match is not null)
            {
                return match.ColorHex;
            }
        }

        return _colors.Count > 0 ? _colors[0].ColorHex : "ffffff";
    }

    public async Task ReloadAsync(CancellationToken ct)
    {
        _baseParts = [];
        _layerParts = [];
        _colors = [];

        TurboDbContext dbCtx = await _dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        try
        {
            List<GroupBadgePartEntity> parts = await dbCtx
                .GroupBadgeParts.AsNoTracking()
                .Where(p => p.Enabled)
                .OrderBy(p => p.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            _baseParts = parts
                .Where(p => p.Type == "base")
                .Select(p => new GroupBadgePartOptionSnapshot
                {
                    Id = p.PartId,
                    FileName = p.FileName,
                    MaskFileName = p.MaskFileName,
                })
                .ToList();

            _layerParts = parts
                .Where(p => p.Type == "symbol")
                .Select(p => new GroupBadgePartOptionSnapshot
                {
                    Id = p.PartId,
                    FileName = p.FileName,
                    MaskFileName = p.MaskFileName,
                })
                .ToList();

            List<GroupColorEntity> colors = await dbCtx
                .GroupColors.AsNoTracking()
                .OrderBy(c => c.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            _colors = colors
                .Select(c => new GroupColorOptionSnapshot { Id = c.ColorId, ColorHex = c.ColorHex })
                .ToList();

            _logger.LogInformation(
                "Loaded group badge parts: {Bases} bases, {Symbols} symbols, {Colors} colors",
                _baseParts.Count,
                _layerParts.Count,
                _colors.Count
            );
        }
        finally
        {
            await dbCtx.DisposeAsync().ConfigureAwait(false);
        }
    }
}
