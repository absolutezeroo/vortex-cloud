using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Turbo.Database.Context;
using Turbo.Database.Entities.Permissions;
using Turbo.Primitives.Permissions;

namespace Turbo.Authentication.Permissions;

/// <summary>
/// Resolves admin-configured <see cref="SanctionPresetEntity"/> rows for the staff CFH tool's
/// ModBan/ModTradingLock dropdowns. Read-mostly, low call volume (one lookup per sanction applied) —
/// no caching needed, unlike <see cref="PermissionService"/>.
/// </summary>
public sealed class SanctionPresetService(IDbContextFactory<TurboDbContext> dbContextFactory)
    : ISanctionPresetService
{
    private readonly IDbContextFactory<TurboDbContext> _dbContextFactory = dbContextFactory;

    public async Task<SanctionPresetSnapshot?> ResolveAsync(
        SanctionPresetKind kind,
        int presetIndex,
        CancellationToken ct = default
    )
    {
        await using TurboDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        SanctionPresetEntity? preset = await dbCtx
            .SanctionPresets.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Kind == kind && p.PresetIndex == presetIndex && p.DeletedAt == null,
                ct
            )
            .ConfigureAwait(false);

        return preset is null
            ? null
            : new SanctionPresetSnapshot(preset.Name, preset.DurationSeconds, preset.Message);
    }

    public async Task<SanctionPresetSnapshot?> ResolveByIdAsync(
        int presetId,
        CancellationToken ct = default
    )
    {
        await using TurboDbContext dbCtx = await _dbContextFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        SanctionPresetEntity? preset = await dbCtx
            .SanctionPresets.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == presetId && p.DeletedAt == null, ct)
            .ConfigureAwait(false);

        return preset is null
            ? null
            : new SanctionPresetSnapshot(preset.Name, preset.DurationSeconds, preset.Message);
    }
}
