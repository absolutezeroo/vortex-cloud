using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Room;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;

namespace Vortex.Rooms.Grains;

/// <summary>
/// The single writer of one shared wired variable's values.
/// </summary>
/// <remarks>
/// Rows are cached on activation and the cache is kept authoritative by write-through, the same
/// shape as <c>ServerConfigGrain</c>: every reader goes through this one activation, so a value
/// written from one room is what the next room to ask is told, with no reload dance.
/// <para>
/// The rooms do not call this on the read path — <c>IWiredVariable.TryGetValue</c> is synchronous by
/// contract — they keep replicas and refresh them from here. So a room sees another room's write
/// after its next refresh, not instantly. That is the trade a shared variable makes for being
/// readable from a room that is not the one holding it.
/// </para>
/// </remarks>
internal sealed class WiredSharedVariableGrain(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    ILogger<WiredSharedVariableGrain> logger
) : Grain, IWiredSharedVariableGrain
{
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly ILogger<WiredSharedVariableGrain> _logger = logger;

    private readonly Dictionary<string, WiredSharedValueSnapshot> _cache = [];

    /// <summary>The variable this activation is, in the decimal form every other surface writes it
    /// in — the wire, the box configuration and the shared-variable index all use
    /// <c>WiredVariableId.ToString()</c>, so the rows match what a reader would look for.</summary>
    private string VariableId =>
        unchecked((ulong)this.GetPrimaryKeyLong()).ToString(CultureInfo.InvariantCulture);

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        await LoadAsync(ct).ConfigureAwait(true);
        await base.OnActivateAsync(ct).ConfigureAwait(true);
    }

    public Task<ImmutableArray<WiredSharedValueSnapshot>> GetAllAsync(CancellationToken ct) =>
        Task.FromResult(_cache.Values.ToImmutableArray());

    public async Task<bool> GiveAsync(
        string storageKey,
        int value,
        bool replace,
        CancellationToken ct
    )
    {
        if (_cache.ContainsKey(storageKey) && !replace)
        {
            return false;
        }

        return await WriteAsync(storageKey, value, ct).ConfigureAwait(true);
    }

    public async Task<bool> SetAsync(string storageKey, int value, CancellationToken ct)
    {
        if (!_cache.ContainsKey(storageKey))
        {
            return false;
        }

        return await WriteAsync(storageKey, value, ct).ConfigureAwait(true);
    }

    public async Task<bool> RemoveAsync(string storageKey, CancellationToken ct)
    {
        if (!_cache.ContainsKey(storageKey))
        {
            return false;
        }

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            // Tracked rather than ExecuteDelete: the in-memory provider the tests run on does not
            // implement the bulk operations, and one row by primary key is not worth a provider
            // split.
            WiredSharedVariableEntity? row = await dbCtx
                .WiredSharedVariables.FirstOrDefaultAsync(
                    v => v.VariableId == VariableId && v.StorageKey == storageKey,
                    ct
                )
                .ConfigureAwait(true);

            if (row is not null)
            {
                dbCtx.WiredSharedVariables.Remove(row);

                await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
            }

            // Only once the row is actually gone. Dropping it from the cache first would report a
            // deletion the database refused, and the value would reappear at the next activation.
            _cache.Remove(storageKey);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to delete shared wired variable {VariableId} key {StorageKey}.",
                VariableId,
                storageKey
            );

            return false;
        }
    }

    private async Task<bool> WriteAsync(string storageKey, int value, CancellationToken ct)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long createdAtMs = _cache.TryGetValue(storageKey, out WiredSharedValueSnapshot? existing)
            ? existing.CreatedAtMs
            : now;

        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            WiredSharedVariableEntity? row = await dbCtx
                .WiredSharedVariables.FirstOrDefaultAsync(
                    v => v.VariableId == VariableId && v.StorageKey == storageKey,
                    ct
                )
                .ConfigureAwait(true);

            if (row is null)
            {
                dbCtx.WiredSharedVariables.Add(
                    new WiredSharedVariableEntity
                    {
                        VariableId = VariableId,
                        StorageKey = storageKey,
                        Value = value,
                        CreatedAtMs = createdAtMs,
                        UpdatedAtMs = now,
                    }
                );
            }
            else
            {
                row.Value = value;
                row.UpdatedAtMs = now;
            }

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            // The cache is not updated: a value the database refused is a value no other room will
            // see, and reporting success here would let the two sides disagree permanently.
            _logger.LogError(
                ex,
                "Failed to write shared wired variable {VariableId} key {StorageKey}.",
                VariableId,
                storageKey
            );

            return false;
        }

        _cache[storageKey] = new WiredSharedValueSnapshot
        {
            StorageKey = storageKey,
            Value = value,
            CreatedAtMs = createdAtMs,
            UpdatedAtMs = now,
        };

        return true;
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            List<WiredSharedVariableEntity> rows = await dbCtx
                .WiredSharedVariables.AsNoTracking()
                .Where(v => v.VariableId == VariableId && v.DeletedAt == null)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            foreach (WiredSharedVariableEntity row in rows)
            {
                _cache[row.StorageKey] = new WiredSharedValueSnapshot
                {
                    StorageKey = row.StorageKey,
                    Value = row.Value,
                    CreatedAtMs = row.CreatedAtMs,
                    UpdatedAtMs = row.UpdatedAtMs,
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to load shared wired variable {VariableId}; it starts empty.",
                VariableId
            );
        }
    }
}
