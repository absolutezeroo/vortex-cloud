using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Fishing;
using Vortex.Primitives.Fishing;
using Vortex.Primitives.Fishing.Grains;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players;
using Vortex.Protocol.Messages.Outgoing.Fishing;

namespace Vortex.Fishing.Grains;

/// <summary>
/// The hotel's fishing derby and its leaderboard.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Vortex's own addition, not an Origins feature.</strong> Origins has the Fishing Frenzy —
/// every four hours, every catch triggers Hook Havoc and XP is ×5 — but no leaderboard contest. The
/// derby is the "concours" this hotel asked for, kept honestly separate from the reconstruction. See
/// the client's <c>docs/vortex-original/fishing.md</c>.
/// </para>
/// <para>
/// A singleton, because one derby runs at a time and the board is hotel-wide. The running derby is
/// re-read from the database rather than cached: an operator schedules these by writing a row, and a
/// cache would mean a derby does not start until the silo is restarted.
/// </para>
/// <para>
/// <strong>Client status.</strong> The two derby packets are registered on both sides, but the
/// client has no UI for them yet: nothing sends <c>JoinDerby</c> and nothing listens for a standing.
/// The server half is complete and correct on its own — it simply has no caller until that UI is
/// built.
/// </para>
/// </remarks>
[KeepAlive]
internal sealed class FishingDerbyGrain(
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    IGrainFactory grainFactory,
    ILogger<FishingDerbyGrain> logger
) : Grain, IFishingDerbyGrain
{
    private readonly IDbContextFactory<VortexDbContext> _dbCtxFactory = dbCtxFactory;
    private readonly IGrainFactory _grainFactory = grainFactory;
    private readonly ILogger<FishingDerbyGrain> _logger = logger;

    public async Task<FishingDerbySnapshot?> GetCurrentAsync(CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        FishingDerbyEntity? derby = await FindRunningAsync(dbCtx, ct).ConfigureAwait(true);

        if (derby is null)
        {
            return null;
        }

        return await BuildSnapshotAsync(dbCtx, derby, ct).ConfigureAwait(true);
    }

    public async Task<bool> JoinAsync(PlayerId playerId, int derbyId, CancellationToken ct)
    {
        await using VortexDbContext dbCtx = await _dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        FishingDerbyEntity? derby = await FindRunningAsync(dbCtx, ct).ConfigureAwait(true);

        // The id has to match as well as a derby having to be running: a stale click should not
        // silently enter the player into whatever contest happens to be on now.
        if (derby is null || derby.Id != derbyId)
        {
            await SendAsync(
                    playerId,
                    new VortexFishingErrorMessageComposer
                    {
                        Code = (int)FishingErrorCode.DerbyClosed,
                    },
                    ct
                )
                .ConfigureAwait(true);

            return false;
        }

        bool joined = await dbCtx
            .FishingDerbyEntries.AnyAsync(
                entry => entry.DerbyId == derby.Id && entry.PlayerId == playerId.Value,
                ct
            )
            .ConfigureAwait(true);

        if (!joined)
        {
            // A row from the moment they join, scoring zero. A board that hides its entrants until
            // they score reads as broken to the person who just entered.
            dbCtx.FishingDerbyEntries.Add(
                new FishingDerbyEntryEntity { DerbyId = derby.Id, PlayerId = playerId.Value }
            );

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
        }

        await PushStandingAsync(dbCtx, derby, playerId, ct).ConfigureAwait(true);

        return true;
    }

    public async Task OfferCatchAsync(PlayerId playerId, int weight, CancellationToken ct)
    {
        try
        {
            await using VortexDbContext dbCtx = await _dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            FishingDerbyEntity? derby = await FindRunningAsync(dbCtx, ct).ConfigureAwait(true);

            if (derby is null)
            {
                return;
            }

            FishingDerbyEntryEntity? entry = await dbCtx
                .FishingDerbyEntries.FirstOrDefaultAsync(
                    row => row.DerbyId == derby.Id && row.PlayerId == playerId.Value,
                    ct
                )
                .ConfigureAwait(true);

            // Not joined, or the catch does not beat their own best. Either way there is nothing to
            // write and nothing to tell them.
            if (entry is null || weight <= entry.BestWeight)
            {
                return;
            }

            entry.BestWeight = weight;

            await dbCtx.SaveChangesAsync(ct).ConfigureAwait(true);
            await PushStandingAsync(dbCtx, derby, playerId, ct).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            // The catch itself is already banked. A derby that failed to record it is worth a line
            // in the log and nothing more — throwing here would fail the catch retroactively.
            _logger.LogWarning(
                ex,
                "Failed to record a derby score for player {PlayerId}",
                playerId
            );
        }
    }

    /// <summary>The derby whose window contains now, or null — which is the ordinary state.</summary>
    private static async Task<FishingDerbyEntity?> FindRunningAsync(
        VortexDbContext dbCtx,
        CancellationToken ct
    )
    {
        DateTime now = DateTime.UtcNow;

        return await dbCtx
            .FishingDerbies.AsNoTracking()
            .Where(derby => derby.DeletedAt == null && derby.StartsAt <= now && derby.EndsAt > now)
            .OrderByDescending(derby => derby.StartsAt)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(true);
    }

    private async Task<FishingDerbySnapshot> BuildSnapshotAsync(
        VortexDbContext dbCtx,
        FishingDerbyEntity derby,
        CancellationToken ct
    )
    {
        int size = (
            await _grainFactory
                .GetFishingDefinitionsGrain()
                .GetSettingsAsync(ct)
                .ConfigureAwait(true)
        ).DerbyLeaderboardSize;

        // Joined against players for the display name: the leaderboard is the one place a fishing
        // message names somebody other than the reader.
        var rows = await (
            from entry in dbCtx.FishingDerbyEntries.AsNoTracking()
            join player in dbCtx.Players.AsNoTracking() on entry.PlayerId equals player.Id
            where entry.DerbyId == derby.Id
            orderby entry.BestWeight descending
            select new
            {
                entry.PlayerId,
                player.Name,
                entry.BestWeight,
            }
        )
            .Take(Math.Max(1, size))
            .ToArrayAsync(ct)
            .ConfigureAwait(true);

        return new FishingDerbySnapshot
        {
            Id = derby.Id,
            EndsAt = (int)new DateTimeOffset(derby.EndsAt, TimeSpan.Zero).ToUnixTimeSeconds(),
            Entries =
            [
                .. rows.Select(row => new FishingDerbyEntrySnapshot
                {
                    PlayerId = row.PlayerId,
                    PlayerName = row.Name,
                    Score = row.BestWeight,
                }),
            ],
        };
    }

    /// <summary>
    /// Sends one player the board and their own place in it.
    /// </summary>
    /// <remarks>
    /// The rank is counted with a separate query rather than read off the truncated board: a player
    /// outside the visible top still has to be told where they are, and sending the whole board to
    /// work that out would grow with the hotel.
    /// </remarks>
    private async Task PushStandingAsync(
        VortexDbContext dbCtx,
        FishingDerbyEntity derby,
        PlayerId playerId,
        CancellationToken ct
    )
    {
        FishingDerbySnapshot snapshot = await BuildSnapshotAsync(dbCtx, derby, ct)
            .ConfigureAwait(true);

        int ownBest =
            await dbCtx
                .FishingDerbyEntries.AsNoTracking()
                .Where(entry => entry.DerbyId == derby.Id && entry.PlayerId == playerId.Value)
                .Select(entry => (int?)entry.BestWeight)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(true)
            ?? -1;

        int ownRank =
            ownBest < 0
                ? 0
                : await dbCtx
                    .FishingDerbyEntries.AsNoTracking()
                    .CountAsync(
                        entry => entry.DerbyId == derby.Id && entry.BestWeight > ownBest,
                        ct
                    )
                    .ConfigureAwait(true) + 1;

        await SendAsync(
                playerId,
                new VortexFishingDerbyStandingMessageComposer
                {
                    DerbyId = snapshot.Id,
                    EndsAt = snapshot.EndsAt,
                    Entries = snapshot.Entries,
                    OwnRank = ownRank,
                },
                ct
            )
            .ConfigureAwait(true);
    }

    private async Task SendAsync(
        PlayerId playerId,
        Vortex.Primitives.Networking.IComposer composer,
        CancellationToken ct
    )
    {
        try
        {
            await _grainFactory
                .GetPlayerPresenceGrain(playerId)
                .SendComposerAsync(composer)
                .ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not push a derby standing to player {PlayerId}", playerId);
        }
    }
}
