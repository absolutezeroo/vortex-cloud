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
using Vortex.Database.Entities.Habbicons;
using Vortex.Primitives.Commerce;
using Vortex.Primitives.Events;
using Vortex.Primitives.Habbicons;
using Vortex.Primitives.Habbicons.Grains;
using Vortex.Primitives.Habbicons.Snapshots;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Server.Grains;
using Vortex.Protocol.Messages.Outgoing.Habbicons;

namespace Vortex.Habbicons.Grains;

/// <summary>
/// One player's Habbicons.
/// </summary>
/// <remarks>
/// <para>
/// The ownership map is hydrated once on activation and kept in step with every write here, because
/// every read in the feature goes through it: the shop resolves against it, a use checks it, and a
/// collection's completion is computed from it. A grain-per-player also means the whole
/// read-decide-write of a purchase or a claim runs in one turn, so the double-click that used to be
/// a race is just a second turn finding the work already done.
/// </para>
/// <para>
/// Definitions are not here. They live in the process-wide <see cref="IHabbiconCatalog"/>, so this
/// grain holds only what is true of this player.
/// </para>
/// </remarks>
internal sealed partial class PlayerHabbiconGrain(
    IGrainFactory grainFactory,
    IDbContextFactory<VortexDbContext> dbCtxFactory,
    IHabbiconCatalog catalog,
    IEventPublisher events,
    ICommerceJournal journal,
    ILogger<PlayerHabbiconGrain> logger
) : Grain, IPlayerHabbiconGrain
{
    private const string RecentLimitKey = "habbicons.recent_limit";
    private const string UseCooldownKey = "habbicons.use_cooldown_ms";

    private readonly Dictionary<int, OwnedHabbicon> _owned = [];

    private int _recentLimit = 10;
    private int _useCooldownMs = 500;
    private long _lastUseMs;
    private bool _loaded;

    private int PlayerId => (int)this.GetPrimaryKeyLong();

    private IPlayerPresenceGrain Presence => grainFactory.GetPlayerPresenceGrain(PlayerId);

    /// <summary>A stored ownership row, reduced to what any decision here actually needs.</summary>
    private sealed record OwnedHabbicon(
        HabbiconState State,
        DateTime AcquiredAt,
        HabbiconSource Source,
        DateTime? LastUsedAt
    );

    public override async Task OnActivateAsync(CancellationToken ct)
    {
        await LoadAsync(ct).ConfigureAwait(true);
        await ReadLimitsAsync().ConfigureAwait(true);
        await base.OnActivateAsync(ct).ConfigureAwait(true);
    }

    public Task<HabbiconInventorySnapshot> GetInventoryAsync(CancellationToken ct) =>
        Task.FromResult(BuildInventory());

    public Task<HabbiconShopSnapshot> GetShopAsync(CancellationToken ct) =>
        Task.FromResult(BuildShop());

    public Task<bool> OwnsAsync(int habbiconId, CancellationToken ct) =>
        Task.FromResult(
            _owned.TryGetValue(habbiconId, out OwnedHabbicon? row)
                && HabbiconCollectionRules.IsUsable(row.State)
        );

    public async Task PushInventoryAsync(CancellationToken ct)
    {
        HabbiconInventorySnapshot inventory = BuildInventory();

        await Presence
            .SendComposerAsync(
                new UserHabbiconsMessageComposer
                {
                    Habbicons = inventory.Habbicons,
                    RecentHabbiconIds = inventory.RecentHabbiconIds,
                }
            )
            .ConfigureAwait(true);

        // The client asks for the shop itself when the hub opens, but it also redraws the album from
        // whatever it was last sent -- so pushing both at login is what makes the toolbar's unseen
        // count right before anyone opens anything.
        await SendShopAsync().ConfigureAwait(true);
    }

    public async Task SendHabbiconInfoAsync(int habbiconId, CancellationToken ct)
    {
        if (!catalog.TryGetHabbicon(habbiconId, out HabbiconDefinitionSnapshot? definition))
        {
            return;
        }

        await Presence
            .SendComposerAsync(
                new HabbiconInfoMessageComposer { Habbicon = ToShopItem(definition) }
            )
            .ConfigureAwait(true);
    }

    /// <summary>
    /// Whether the ownership map is trustworthy. False after a failed load, and every write path
    /// refuses while it is: an empty map reads as a player who owns nothing, and a grant against
    /// that would write a row the database already has.
    /// </summary>
    private async Task<bool> EnsureLoadedAsync(CancellationToken ct)
    {
        if (_loaded)
        {
            return true;
        }

        await LoadAsync(ct).ConfigureAwait(true);

        return _loaded;
    }

    public async Task<HabbiconGrantResult> GrantAsync(
        int habbiconId,
        HabbiconSource source,
        CancellationToken ct
    )
    {
        if (!catalog.TryGetHabbicon(habbiconId, out HabbiconDefinitionSnapshot? definition))
        {
            logger.LogWarning(
                "Refused to grant unknown Habbicon {HabbiconId} to player {PlayerId} from {Source}.",
                habbiconId,
                PlayerId,
                source
            );

            return HabbiconGrantResult.Failed;
        }

        if (!await EnsureLoadedAsync(ct).ConfigureAwait(true))
        {
            // The ownership map could not be read, so "do they already have this?" has no answer.
            // Refusing is the only safe reply: granting would risk a duplicate row and reporting
            // "already owned" would silently swallow a real reward.
            logger.LogError(
                "Refused to grant Habbicon {HabbiconId} to player {PlayerId}: ownership is unknown.",
                habbiconId,
                PlayerId
            );

            return HabbiconGrantResult.Failed;
        }

        // Already owned is a success with nothing to do. Every grant path in the hotel leans on
        // this: a reward track handing out a Habbicon the player bought last week must not fail,
        // and must not write a second row either.
        if (_owned.TryGetValue(habbiconId, out OwnedHabbicon? existing))
        {
            return new HabbiconGrantResult
            {
                Succeeded = true,
                WasNew = false,
                State = existing.State,
            };
        }

        DateTime now = DateTime.UtcNow;

        await using (
            VortexDbContext db = await dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(true)
        )
        {
            db.PlayerHabbicons.Add(
                new PlayerHabbiconEntity
                {
                    PlayerEntityId = PlayerId,
                    HabbiconEntityId = habbiconId,
                    State = HabbiconState.Owned,
                    Source = source,
                    AcquiredAt = now,
                }
            );

            await db.SaveChangesAsync(ct).ConfigureAwait(true);
        }

        _owned[habbiconId] = new OwnedHabbicon(HabbiconState.Owned, now, source, null);

        await NotifyStateAsync(habbiconId, HabbiconState.Owned).ConfigureAwait(true);

        await events
            .PublishAsync(
                new HabbiconGrantedEvent(PlayerId, habbiconId, definition.CollectionId, source),
                ct
            )
            .ConfigureAwait(true);

        logger.LogInformation(
            "Granted Habbicon {HabbiconId} ({Code}) to player {PlayerId} from {Source}.",
            habbiconId,
            definition.Code,
            PlayerId,
            source
        );

        await RaiseCollectionCompletionIfNewAsync(definition, ct).ConfigureAwait(true);

        return new HabbiconGrantResult
        {
            Succeeded = true,
            WasNew = true,
            State = HabbiconState.Owned,
        };
    }

    public async Task<bool> RevokeAsync(int habbiconId, CancellationToken ct)
    {
        if (!_owned.ContainsKey(habbiconId))
        {
            return false;
        }

        await using (
            VortexDbContext db = await dbCtxFactory.CreateDbContextAsync(ct).ConfigureAwait(true)
        )
        {
            PlayerHabbiconEntity? row = await db
                .PlayerHabbicons.FirstOrDefaultAsync(
                    h => h.PlayerEntityId == PlayerId && h.HabbiconEntityId == habbiconId,
                    ct
                )
                .ConfigureAwait(true);

            if (row is not null)
            {
                db.PlayerHabbicons.Remove(row);
                await db.SaveChangesAsync(ct).ConfigureAwait(true);
            }
        }

        _owned.Remove(habbiconId);

        // NotOwned is how the client is told to delete its row: it keeps a row for any state it
        // considers stored and drops it for anything else.
        await NotifyStateAsync(habbiconId, HabbiconState.NotOwned).ConfigureAwait(true);
        await SendShopAsync().ConfigureAwait(true);

        logger.LogInformation(
            "Revoked Habbicon {HabbiconId} from player {PlayerId}.",
            habbiconId,
            PlayerId
        );

        return true;
    }

    public async Task SetFavouriteAsync(int habbiconId, bool favourite, CancellationToken ct)
    {
        if (
            !_owned.TryGetValue(habbiconId, out OwnedHabbicon? row)
            || !HabbiconCollectionRules.IsUsable(row.State)
        )
        {
            return;
        }

        HabbiconState next = favourite ? HabbiconState.Favourite : HabbiconState.Owned;

        if (row.State == next)
        {
            return;
        }

        await UpdateStateAsync(habbiconId, next, ct).ConfigureAwait(true);
        await NotifyStateAsync(habbiconId, next).ConfigureAwait(true);
    }

    /// <summary>
    /// Reloads the ownership map from the database. Used on activation, and after a purchase writes
    /// several rows at once.
    /// </summary>
    private async Task LoadAsync(CancellationToken ct)
    {
        try
        {
            await using VortexDbContext db = await dbCtxFactory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(true);

            List<PlayerHabbiconEntity> rows = await db
                .PlayerHabbicons.AsNoTracking()
                .Where(h => h.PlayerEntityId == PlayerId && h.DeletedAt == null)
                .ToListAsync(ct)
                .ConfigureAwait(true);

            _owned.Clear();

            foreach (PlayerHabbiconEntity row in rows)
            {
                _owned[row.HabbiconEntityId] = new OwnedHabbicon(
                    row.State,
                    row.AcquiredAt,
                    row.Source,
                    row.LastUsedAt
                );
            }

            _loaded = true;
        }
        catch (Exception ex)
        {
            // Left unloaded on purpose rather than pretending the player owns nothing: an empty map
            // would let the album show a wiped collection and let a re-grant write a duplicate row.
            logger.LogError(
                ex,
                "Failed to load Habbicon ownership for player {PlayerId}.",
                PlayerId
            );
        }
    }

    private async Task ReadLimitsAsync()
    {
        try
        {
            IServerConfigGrain config = grainFactory.GetServerConfigGrain();

            _recentLimit = Math.Max(
                0,
                await config.GetIntAsync(RecentLimitKey, 10).ConfigureAwait(true)
            );
            _useCooldownMs = Math.Max(
                0,
                await config.GetIntAsync(UseCooldownKey, 500).ConfigureAwait(true)
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read Habbicon limits; keeping the defaults.");
        }
    }

    private async Task UpdateStateAsync(int habbiconId, HabbiconState state, CancellationToken ct)
    {
        await using VortexDbContext db = await dbCtxFactory
            .CreateDbContextAsync(ct)
            .ConfigureAwait(true);

        PlayerHabbiconEntity? row = await db
            .PlayerHabbicons.FirstOrDefaultAsync(
                h => h.PlayerEntityId == PlayerId && h.HabbiconEntityId == habbiconId,
                ct
            )
            .ConfigureAwait(true);

        if (row is null)
        {
            return;
        }

        row.State = state;
        await db.SaveChangesAsync(ct).ConfigureAwait(true);

        if (_owned.TryGetValue(habbiconId, out OwnedHabbicon? cached))
        {
            _owned[habbiconId] = cached with { State = state };
        }
    }

    private Task NotifyStateAsync(int habbiconId, HabbiconState state) =>
        Presence.SendComposerAsync(
            new UserHabbiconStatusChangedMessageComposer { HabbiconId = habbiconId, State = state }
        );

    private Task SendShopAsync() =>
        Presence.SendComposerAsync(new HabbiconShopDataMessageComposer { Shop = BuildShop() });

    private HabbiconInventorySnapshot BuildInventory() =>
        new()
        {
            Habbicons =
            [
                .. _owned.Select(kv => new PlayerHabbiconSnapshot
                {
                    HabbiconId = kv.Key,
                    State = kv.Value.State,
                    AcquiredAtUtc = kv.Value.AcquiredAt,
                    Source = kv.Value.Source,
                }),
            ],
            RecentHabbiconIds =
            [
                .. _owned
                    .Where(kv => kv.Value.LastUsedAt is not null)
                    .OrderByDescending(kv => kv.Value.LastUsedAt)
                    .Take(_recentLimit)
                    .Select(kv => kv.Key),
            ],
        };

    private HabbiconShopSnapshot BuildShop()
    {
        DateTime now = DateTime.UtcNow;
        List<HabbiconShopCollectionSnapshot> collections = [];

        foreach (HabbiconCollectionSnapshot collection in catalog.Collections)
        {
            if (!IsVisible(collection, now))
            {
                continue;
            }

            collections.Add(
                new HabbiconShopCollectionSnapshot
                {
                    CollectionId = collection.CollectionId,
                    Code = collection.Code,
                    Completed = HabbiconCollectionRules.IsComplete(collection, StateMap()),
                    RewardHabbiconId = collection.RewardHabbicon?.HabbiconId ?? 0,
                    RewardState = HabbiconCollectionRules.ResolveRewardState(
                        collection,
                        StateMap()
                    ),
                    PriceCredits = collection.PriceCredits,
                    PriceActivityPoints = collection.PriceActivityPoints,
                    ActivityPointType = collection.ActivityPointType,
                    Habbicons = [.. collection.Entries.Select(ToShopItem)],
                }
            );
        }

        return new HabbiconShopSnapshot { Collections = [.. collections] };
    }

    /// <summary>
    /// A hidden or expired collection is still shown to a player who owns something in it —
    /// otherwise last season's set would vanish out of their album along with the picture of what
    /// they collected.
    /// </summary>
    private bool IsVisible(HabbiconCollectionSnapshot collection, DateTime now)
    {
        if (collection.IsAvailableAt(now) && !collection.Hidden)
        {
            return true;
        }

        foreach (HabbiconDefinitionSnapshot entry in collection.Entries)
        {
            if (_owned.ContainsKey(entry.HabbiconId))
            {
                return true;
            }
        }

        return collection.RewardHabbicon is not null
            && _owned.ContainsKey(collection.RewardHabbicon.HabbiconId);
    }

    private HabbiconShopItemSnapshot ToShopItem(HabbiconDefinitionSnapshot definition) =>
        new()
        {
            HabbiconId = definition.HabbiconId,
            Code = definition.Code,
            CollectionId = definition.CollectionId,
            State = _owned.TryGetValue(definition.HabbiconId, out OwnedHabbicon? row)
                ? row.State
                : HabbiconState.NotOwned,
            PriceCredits = definition.PriceCredits,
            PriceActivityPoints = definition.PriceActivityPoints,
            ActivityPointType = definition.ActivityPointType,
        };

    /// <summary>The ownership map in the shape the pure rules take.</summary>
    private Dictionary<int, HabbiconState> StateMap() =>
        _owned.ToDictionary(kv => kv.Key, kv => kv.Value.State);

    /// <summary>
    /// Publishes the collection-completed event when <paramref name="granted"/> was the piece that
    /// finished its set. Checked here rather than on every read so it fires once, on the grant that
    /// did it.
    /// </summary>
    private async Task RaiseCollectionCompletionIfNewAsync(
        HabbiconDefinitionSnapshot granted,
        CancellationToken ct
    )
    {
        if (
            granted.IsCollectionReward
            || !catalog.TryGetCollection(
                granted.CollectionId,
                out HabbiconCollectionSnapshot? collection
            )
        )
        {
            return;
        }

        Dictionary<int, HabbiconState> states = StateMap();

        if (!HabbiconCollectionRules.IsComplete(collection, states))
        {
            return;
        }

        // The bonus becomes claimable at the same instant. Telling the client now means the album
        // lights up without waiting for the next shop request.
        if (
            collection.RewardHabbicon is not null
            && !_owned.ContainsKey(collection.RewardHabbicon.HabbiconId)
        )
        {
            await NotifyStateAsync(collection.RewardHabbicon.HabbiconId, HabbiconState.Claimable)
                .ConfigureAwait(true);
        }

        await SendShopAsync().ConfigureAwait(true);

        await events
            .PublishAsync(
                new HabbiconCollectionCompletedEvent(
                    PlayerId,
                    collection.CollectionId,
                    collection.Code,
                    collection.RewardHabbicon?.HabbiconId ?? 0
                ),
                ct
            )
            .ConfigureAwait(true);

        logger.LogInformation(
            "Player {PlayerId} completed Habbicon collection {CollectionCode}.",
            PlayerId,
            collection.Code
        );
    }
}
