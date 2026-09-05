using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Events.Registry;
using Vortex.Primitives.Commerce;
using Vortex.Primitives.Events;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.RewardTracks;

namespace Vortex.RewardTracks.Events;

/// <summary>
/// The bridge from gameplay to reward tracks.
/// </summary>
/// <remarks>
/// <para>
/// Every handler here does the same three things: check the index, translate the event into an
/// action code with an amount and a target, and hand it to the player's grain. None of them knows
/// what a task is, which track might care, or whether any content exists — that is the point.
/// Nothing on the producing side has heard of reward tracks either: a room publishes
/// <c>PlayerEnteredRoomEvent</c> because a player entered a room.
/// </para>
/// <para>
/// The index check comes first and is the reason this scales. It answers from a hash set in this
/// thread, so an event no campaign mentions costs a lookup and never reaches a grain — which
/// matters when the event is a chat line.
/// </para>
/// <para>
/// Grouped in one file, like the quest module's own bridge: each handler is six lines of
/// translation, and reading them together is how you see the whole vocabulary at once.
/// </para>
/// </remarks>
internal static class RewardTrackSignal
{
    /// <summary>
    /// Sends one signal, if any content is listening for it.
    /// </summary>
    /// <remarks>
    /// The guard is not an optimisation detail, it is the contract: a hotel running no campaigns
    /// pays nothing for any of this.
    /// </remarks>
    public static Task SendAsync(
        IGrainFactory grainFactory,
        IRewardTrackCatalog catalog,
        long playerId,
        string actionCode,
        int amount,
        string? target,
        CancellationToken ct
    )
    {
        if (playerId <= 0 || !catalog.IsActionInteresting(actionCode))
        {
            return Task.CompletedTask;
        }

        return grainFactory
            .GetPlayerRewardTrackGrain(playerId)
            .ProgressAsync(actionCode, amount, target, ct);
    }
}

/// <summary>
/// Room entries. The target is the room id, which is what a distinct-mode task deduplicates on —
/// "visit 20 different rooms" counts twenty rooms, not twenty doorways.
/// </summary>
public sealed class RewardTrackRoomEntryHandler(
    IGrainFactory grainFactory,
    IRewardTrackCatalog catalog
) : IEventHandler<PlayerEnteredRoomEvent>
{
    public async ValueTask HandleAsync(
        PlayerEnteredRoomEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await RewardTrackSignal
            .SendAsync(
                grainFactory,
                catalog,
                e.PlayerId.Value,
                RewardTrackActions.EnterOtherUsersRoom,
                1,
                e.RoomId.ToString(CultureInfo.InvariantCulture),
                ct
            )
            .ConfigureAwait(false);
}

/// <summary>
/// Chat the room accepted. Deliberately <c>PlayerChattedEvent</c> and not the cancellable
/// <c>PlayerChattingEvent</c>: the latter fires for a line a behaviour then drops, and paying for
/// words nobody heard is exactly what the "only successful actions progress" rule forbids.
/// </summary>
public sealed class RewardTrackChatHandler(IGrainFactory grainFactory, IRewardTrackCatalog catalog)
    : IEventHandler<PlayerChattedEvent>
{
    public async ValueTask HandleAsync(PlayerChattedEvent e, EventContext ctx, CancellationToken ct)
    {
        // Whispers do not count. A "chat with users" task is about talking to a room, and a whisper
        // to yourself would otherwise farm it.
        if (e.Whisper)
        {
            return;
        }

        await RewardTrackSignal
            .SendAsync(
                grainFactory,
                catalog,
                e.PlayerId.Value,
                RewardTrackActions.ChatWithSomeone,
                1,
                null,
                ct
            )
            .ConfigureAwait(false);
    }
}

/// <summary>Dances and waves, each on its own action code.</summary>
public sealed class RewardTrackGestureHandler(
    IGrainFactory grainFactory,
    IRewardTrackCatalog catalog
) : IEventHandler<PlayerGesturedEvent>
{
    public async ValueTask HandleAsync(
        PlayerGesturedEvent e,
        EventContext ctx,
        CancellationToken ct
    )
    {
        string? action = e.Gesture switch
        {
            "dance" => RewardTrackActions.Dance,
            "wave" => RewardTrackActions.Wave,
            _ => null,
        };

        if (action is null)
        {
            return;
        }

        await RewardTrackSignal
            .SendAsync(grainFactory, catalog, e.PlayerId.Value, action, 1, null, ct)
            .ConfigureAwait(false);
    }
}

/// <summary>Friend requests sent. The asking, not the accepting — the client's own task says "make friends".</summary>
public sealed class RewardTrackFriendRequestHandler(
    IGrainFactory grainFactory,
    IRewardTrackCatalog catalog
) : IEventHandler<FriendRequestSentEvent>
{
    public async ValueTask HandleAsync(
        FriendRequestSentEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await RewardTrackSignal
            .SendAsync(
                grainFactory,
                catalog,
                e.ActorPlayerId,
                RewardTrackActions.RequestFriend,
                1,
                e.TargetPlayerId.ToString(CultureInfo.InvariantCulture),
                ct
            )
            .ConfigureAwait(false);
}

/// <summary>Respect given. The target is who received it, so a distinct task can require different people.</summary>
public sealed class RewardTrackRespectHandler(
    IGrainFactory grainFactory,
    IRewardTrackCatalog catalog
) : IEventHandler<RespectGivenEvent>
{
    public async ValueTask HandleAsync(
        RespectGivenEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await RewardTrackSignal
            .SendAsync(
                grainFactory,
                catalog,
                e.ActorPlayerId,
                RewardTrackActions.GiveRespect,
                1,
                e.TargetPlayerId.ToString(CultureInfo.InvariantCulture),
                ct
            )
            .ConfigureAwait(false);
}

/// <summary>Figure changes.</summary>
public sealed class RewardTrackFigureHandler(
    IGrainFactory grainFactory,
    IRewardTrackCatalog catalog
) : IEventHandler<PlayerFigureChangedEvent>
{
    public async ValueTask HandleAsync(
        PlayerFigureChangedEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await RewardTrackSignal
            .SendAsync(
                grainFactory,
                catalog,
                e.PlayerId.Value,
                RewardTrackActions.ChangeFigure,
                1,
                null,
                ct
            )
            .ConfigureAwait(false);
}

/// <summary>Motto changes.</summary>
public sealed class RewardTrackMottoHandler(IGrainFactory grainFactory, IRewardTrackCatalog catalog)
    : IEventHandler<PlayerMottoChangedEvent>
{
    public async ValueTask HandleAsync(
        PlayerMottoChangedEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await RewardTrackSignal
            .SendAsync(
                grainFactory,
                catalog,
                e.PlayerId.Value,
                RewardTrackActions.ChangeMotto,
                1,
                null,
                ct
            )
            .ConfigureAwait(false);
}

/// <summary>Badges equipped. One signal per badge worn, so a target can name a specific one.</summary>
public sealed class RewardTrackBadgeHandler(IGrainFactory grainFactory, IRewardTrackCatalog catalog)
    : IEventHandler<BadgesEquippedEvent>
{
    public async ValueTask HandleAsync(
        BadgesEquippedEvent e,
        EventContext ctx,
        CancellationToken ct
    )
    {
        foreach (string badgeCode in e.BadgeCodes)
        {
            await RewardTrackSignal
                .SendAsync(
                    grainFactory,
                    catalog,
                    e.PlayerId.Value,
                    RewardTrackActions.WearBadge,
                    1,
                    badgeCode,
                    ct
                )
                .ConfigureAwait(false);
        }
    }
}

/// <summary>Rooms created.</summary>
public sealed class RewardTrackRoomCreatedHandler(
    IGrainFactory grainFactory,
    IRewardTrackCatalog catalog
) : IEventHandler<RoomCreatedEvent>
{
    public async ValueTask HandleAsync(
        RoomCreatedEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await RewardTrackSignal
            .SendAsync(
                grainFactory,
                catalog,
                e.OwnerId.Value,
                RewardTrackActions.CreateRoom,
                1,
                null,
                ct
            )
            .ConfigureAwait(false);
}

/// <summary>Furniture placed. The target is the definition id, so a task can require a kind of furni.</summary>
public sealed class RewardTrackItemPlacedHandler(
    IGrainFactory grainFactory,
    IRewardTrackCatalog catalog
) : IEventHandler<ItemPlacedEvent>
{
    public async ValueTask HandleAsync(ItemPlacedEvent e, EventContext ctx, CancellationToken ct) =>
        await RewardTrackSignal
            .SendAsync(
                grainFactory,
                catalog,
                e.ActorPlayerId,
                RewardTrackActions.PlaceItem,
                1,
                e.DefinitionId.ToString(CultureInfo.InvariantCulture),
                ct
            )
            .ConfigureAwait(false);
}

/// <summary>Furniture moved within a room.</summary>
public sealed class RewardTrackItemMovedHandler(
    IGrainFactory grainFactory,
    IRewardTrackCatalog catalog
) : IEventHandler<ItemMovedEvent>
{
    public async ValueTask HandleAsync(ItemMovedEvent e, EventContext ctx, CancellationToken ct) =>
        await RewardTrackSignal
            .SendAsync(
                grainFactory,
                catalog,
                e.ActorPlayerId,
                RewardTrackActions.MoveItem,
                1,
                null,
                ct
            )
            .ConfigureAwait(false);
}

/// <summary>
/// Furniture turned on the spot.
/// </summary>
/// <remarks>
/// A rotation is also a move and is deliberately left counting as one: the client has no separate
/// rotate message, every rotation has always raised <see cref="ItemMovedEvent"/>, and quietly
/// excluding them here would make existing <c>move_item</c> tasks count less than the day they were
/// written. <see cref="ItemMovedEvent.RotatedInPlace"/> is what makes the narrower signal possible,
/// not a reclassification of the wider one.
/// </remarks>
public sealed class RewardTrackItemRotatedHandler(
    IGrainFactory grainFactory,
    IRewardTrackCatalog catalog
) : IEventHandler<ItemMovedEvent>
{
    public async ValueTask HandleAsync(ItemMovedEvent e, EventContext ctx, CancellationToken ct)
    {
        if (!e.RotatedInPlace)
        {
            return;
        }

        await RewardTrackSignal
            .SendAsync(
                grainFactory,
                catalog,
                e.ActorPlayerId,
                RewardTrackActions.RotateItem,
                1,
                null,
                ct
            )
            .ConfigureAwait(false);
    }
}

/// <summary>
/// A pet reached a new level.
/// </summary>
/// <remarks>
/// The amount is the level reached, not one: the client's own task is "get a pet to level N", which
/// is <see cref="TaskProgressMode.Highest"/> over the level. A counter-mode task on this action
/// would add levels together and is content's mistake to make, not this handler's to prevent.
/// The credit goes to the pet's owner, who need not be the player who fed it.
/// </remarks>
public sealed class RewardTrackPetLevelHandler(
    IGrainFactory grainFactory,
    IRewardTrackCatalog catalog
) : IEventHandler<PetLeveledUpEvent>
{
    public async ValueTask HandleAsync(
        PetLeveledUpEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await RewardTrackSignal
            .SendAsync(
                grainFactory,
                catalog,
                e.OwnerId.Value,
                RewardTrackActions.PetLevel,
                e.Level,
                e.PetId.ToString(CultureInfo.InvariantCulture),
                ct
            )
            .ConfigureAwait(false);
}

/// <summary>
/// Catalogue purchases. Feeds two action codes: one counting purchases, one counting the credits
/// they cost — which is how "spend 100 credits" is a task without a spending event of its own.
/// </summary>
/// <remarks>
/// Deduplicated by operation. The commerce relay delivers at least once by design, and advancing a
/// task twice for one purchase is the silent wrongness the relay exists to avoid causing.
/// </remarks>
public sealed class RewardTrackCatalogPurchaseHandler(
    IGrainFactory grainFactory,
    IRewardTrackCatalog catalog,
    ICommerceJournal journal
) : IEventHandler<CatalogPurchasedEvent>
{
    public async ValueTask HandleAsync(
        CatalogPurchasedEvent e,
        EventContext ctx,
        CancellationToken ct
    )
    {
        if (
            !await CommerceReplayGuard.FirstDeliveryAsync(
                journal,
                e.OperationId,
                "reward-track",
                ct
            )
        )
        {
            return;
        }

        await RewardTrackSignal
            .SendAsync(
                grainFactory,
                catalog,
                e.PlayerId,
                RewardTrackActions.BuyFromCatalogue,
                e.Quantity > 0 ? e.Quantity : 1,
                e.OfferId.ToString(CultureInfo.InvariantCulture),
                ct
            )
            .ConfigureAwait(false);

        if (e.CreditCost > 0)
        {
            await RewardTrackSignal
                .SendAsync(
                    grainFactory,
                    catalog,
                    e.PlayerId,
                    RewardTrackActions.SpendCredits,
                    e.CreditCost,
                    null,
                    ct
                )
                .ConfigureAwait(false);
        }
    }
}

/// <summary>Completed trades, for both sides.</summary>
public sealed class RewardTrackTradeHandler(IGrainFactory grainFactory, IRewardTrackCatalog catalog)
    : IEventHandler<TradeCompletedEvent>
{
    public async ValueTask HandleAsync(
        TradeCompletedEvent e,
        EventContext ctx,
        CancellationToken ct
    )
    {
        await RewardTrackSignal
            .SendAsync(
                grainFactory,
                catalog,
                e.PlayerOneId,
                RewardTrackActions.CompleteTrade,
                1,
                null,
                ct
            )
            .ConfigureAwait(false);

        await RewardTrackSignal
            .SendAsync(
                grainFactory,
                catalog,
                e.PlayerTwoId,
                RewardTrackActions.CompleteTrade,
                1,
                null,
                ct
            )
            .ConfigureAwait(false);
    }
}

/// <summary>Private messages the messenger accepted.</summary>
public sealed class RewardTrackMessengerHandler(
    IGrainFactory grainFactory,
    IRewardTrackCatalog catalog
) : IEventHandler<MessengerMessageSentEvent>
{
    public async ValueTask HandleAsync(
        MessengerMessageSentEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await RewardTrackSignal
            .SendAsync(
                grainFactory,
                catalog,
                e.PlayerId.Value,
                RewardTrackActions.SendMessengerMessage,
                1,
                null,
                ct
            )
            .ConfigureAwait(false);
}

/// <summary>
/// Habbicons used. The whole of the reward-track side of the Habbicon integration: the Habbicon
/// domain publishes, this subscribes, and neither names a type from the other.
/// </summary>
public sealed class RewardTrackHabbiconUsedHandler(
    IGrainFactory grainFactory,
    IRewardTrackCatalog catalog
) : IEventHandler<HabbiconUsedEvent>
{
    public async ValueTask HandleAsync(
        HabbiconUsedEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await RewardTrackSignal
            .SendAsync(
                grainFactory,
                catalog,
                e.PlayerId.Value,
                RewardTrackActions.UseHabbicon,
                1,
                e.HabbiconId.ToString(CultureInfo.InvariantCulture),
                ct
            )
            .ConfigureAwait(false);
}

/// <summary>Habbicon collections completed. The target is the collection code, so a task can name one.</summary>
public sealed class RewardTrackHabbiconCollectionHandler(
    IGrainFactory grainFactory,
    IRewardTrackCatalog catalog
) : IEventHandler<HabbiconCollectionCompletedEvent>
{
    public async ValueTask HandleAsync(
        HabbiconCollectionCompletedEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await RewardTrackSignal
            .SendAsync(
                grainFactory,
                catalog,
                e.PlayerId.Value,
                RewardTrackActions.CompleteHabbiconCollection,
                1,
                e.CollectionCode,
                ct
            )
            .ConfigureAwait(false);
}

/// <summary>Quests completed.</summary>
public sealed class RewardTrackQuestHandler(IGrainFactory grainFactory, IRewardTrackCatalog catalog)
    : IEventHandler<QuestCompletedEvent>
{
    public async ValueTask HandleAsync(
        QuestCompletedEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await RewardTrackSignal
            .SendAsync(
                grainFactory,
                catalog,
                e.PlayerId.Value,
                RewardTrackActions.CompleteQuest,
                1,
                null,
                ct
            )
            .ConfigureAwait(false);
}

/// <summary>Achievement level-ups.</summary>
public sealed class RewardTrackAchievementHandler(
    IGrainFactory grainFactory,
    IRewardTrackCatalog catalog
) : IEventHandler<AchievementLevelUpEvent>
{
    public async ValueTask HandleAsync(
        AchievementLevelUpEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await RewardTrackSignal
            .SendAsync(
                grainFactory,
                catalog,
                e.PlayerId.Value,
                RewardTrackActions.AchievementLevel,
                1,
                null,
                ct
            )
            .ConfigureAwait(false);
}
