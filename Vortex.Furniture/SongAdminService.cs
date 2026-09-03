using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vortex.Database.Context;
using Vortex.Database.Entities.Gamedata;
using Vortex.Primitives.Sound;
using Vortex.Primitives.Sound.Admin;
using Vortex.Primitives.Sound.Providers;

namespace Vortex.Furniture;

/// <summary>
/// Song catalogue writes, each followed by a reload of the live catalogue.
/// </summary>
/// <remarks>
/// The reload is not a nicety: <see cref="ISongProvider" /> is read on every song the client asks
/// about, and a write that skipped it would leave an operator looking at a row the hotel does not
/// serve — the "DB write not reflected in live state" failure AGENTS.md calls out.
/// </remarks>
internal sealed class SongAdminService(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    ISongProvider songs,
    ILogger<SongAdminService> logger
) : ISongAdminService
{
    public async Task<SongAdminResult> CreateAsync(SongSpec spec, CancellationToken ct)
    {
        if (Validate(spec) is { } invalid)
        {
            return invalid;
        }

        await using VortexDbContext dbCtx = await dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        SongEntity entity = new()
        {
            Name = spec.Name.Trim(),
            Creator = spec.Creator.Trim(),
            LengthMs = spec.LengthMs,
            OfficialSongId = spec.OfficialSongId.Trim(),
            Data = spec.Data,
        };

        dbCtx.Songs.Add(entity);

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadCatalogueAsync(ct).ConfigureAwait(false);

        return SongAdminResult.Ok(entity.Id);
    }

    public async Task<SongAdminResult> UpdateAsync(int songId, SongSpec spec, CancellationToken ct)
    {
        if (Validate(spec) is { } invalid)
        {
            return invalid;
        }

        await using VortexDbContext dbCtx = await dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        SongEntity? entity = await dbCtx
            .Songs.FirstOrDefaultAsync(s => s.Id == songId && s.DeletedAt == null, ct)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return SongAdminResult.Fail("song_not_found");
        }

        entity.Name = spec.Name.Trim();
        entity.Creator = spec.Creator.Trim();
        entity.LengthMs = spec.LengthMs;
        entity.OfficialSongId = spec.OfficialSongId.Trim();
        entity.Data = spec.Data;

        await dbCtx.SaveChangesAsync(ct).ConfigureAwait(false);
        await ReloadCatalogueAsync(ct).ConfigureAwait(false);

        return SongAdminResult.Ok(songId);
    }

    public async Task<SongAdminResult> DeleteAsync(int songId, CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(false);

        // Soft, unlike most of this dashboard's deletes, and safe to be soft because the one reader
        // of this table filters DeletedAt. A disk pressed with a deleted song behaves exactly like a
        // disk for a song the hotel never had, and restoring the row brings it back.
        int rows = await dbCtx
            .Songs.Where(s => s.Id == songId && s.DeletedAt == null)
            .ExecuteUpdateAsync(row => row.SetProperty(s => s.DeletedAt, DateTime.UtcNow), ct)
            .ConfigureAwait(false);

        if (rows == 0)
        {
            return SongAdminResult.Fail("song_not_found");
        }

        await ReloadCatalogueAsync(ct).ConfigureAwait(false);

        return SongAdminResult.Ok(songId);
    }

    public async Task<SongAdminResult> ReloadAsync(CancellationToken ct)
    {
        await ReloadCatalogueAsync(ct).ConfigureAwait(false);

        return SongAdminResult.Ok(0);
    }

    /// <summary>
    /// A song with no name is a nameless entry in every playlist that carries it, and a length of
    /// zero is a song the room's clock steps straight past. Both are cheap to refuse here and
    /// invisible once they are in the table.
    /// </summary>
    private static SongAdminResult? Validate(SongSpec spec)
    {
        if (string.IsNullOrWhiteSpace(spec.Name))
        {
            return SongAdminResult.Fail("name_required");
        }

        if (spec.LengthMs <= 0)
        {
            return SongAdminResult.Fail("length_required");
        }

        return null;
    }

    private async Task ReloadCatalogueAsync(CancellationToken ct)
    {
        try
        {
            await songs.ReloadAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // The row is committed either way; saying so is the difference between an operator
            // wondering why their song is not playing and one who knows to press reload.
            logger.LogError(
                ex,
                "The song catalogue was written but could not be reloaded; the hotel is still serving the previous set."
            );
        }
    }
}
