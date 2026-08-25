using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Events.Registry;
using Vortex.Primitives.Commerce;
using Vortex.Primitives.Events;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Quests;

namespace Vortex.Progression.Quests.Events;

// Daily tasks advance on the same domain events as quests and share their objective vocabulary, so
// a task with quest_type_code "RoomEntry" counts room entries without any new plumbing. They are
// separate handlers rather than a call inside the quest grain: the two systems have different
// lifetimes (a task lapses at midnight, a quest does not) and coupling them would make a failure in
// one silently stop the other. The event pipeline isolates handler exceptions either way.

/// <summary>Advances "RoomEntry" daily tasks.</summary>
public sealed class DailyTaskRoomEntryHandler(IGrainFactory grainFactory)
    : IEventHandler<PlayerEnteredRoomEvent>
{
    public async ValueTask HandleAsync(
        PlayerEnteredRoomEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await grainFactory
            .GetPlayerDailyTaskGrain((long)e.PlayerId)
            .ProgressAsync(QuestTypes.RoomEntry, 1, ct)
            .ConfigureAwait(false);
}

/// <summary>Advances "AvatarLooks" daily tasks.</summary>
public sealed class DailyTaskFigureHandler(IGrainFactory grainFactory)
    : IEventHandler<PlayerFigureChangedEvent>
{
    public async ValueTask HandleAsync(
        PlayerFigureChangedEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await grainFactory
            .GetPlayerDailyTaskGrain((long)e.PlayerId)
            .ProgressAsync(QuestTypes.AvatarLooks, 1, ct)
            .ConfigureAwait(false);
}

/// <summary>Advances "MottoChange" daily tasks.</summary>
public sealed class DailyTaskMottoHandler(IGrainFactory grainFactory)
    : IEventHandler<PlayerMottoChangedEvent>
{
    public async ValueTask HandleAsync(
        PlayerMottoChangedEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await grainFactory
            .GetPlayerDailyTaskGrain((long)e.PlayerId)
            .ProgressAsync(QuestTypes.MottoChange, 1, ct)
            .ConfigureAwait(false);
}

/// <summary>Advances "RespectGiven" daily tasks for the giver.</summary>
public sealed class DailyTaskRespectHandler(IGrainFactory grainFactory)
    : IEventHandler<RespectGivenEvent>
{
    public async ValueTask HandleAsync(
        RespectGivenEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await grainFactory
            .GetPlayerDailyTaskGrain((long)e.ActorPlayerId)
            .ProgressAsync(QuestTypes.RespectGiven, 1, ct)
            .ConfigureAwait(false);
}

/// <summary>Advances "FriendListSize" daily tasks for both sides of an accepted request.</summary>
public sealed class DailyTaskFriendHandler(IGrainFactory grainFactory)
    : IEventHandler<FriendRequestAcceptedEvent>
{
    public async ValueTask HandleAsync(
        FriendRequestAcceptedEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await Task.WhenAll(
                grainFactory
                    .GetPlayerDailyTaskGrain((long)e.ActorPlayerId)
                    .ProgressAsync(QuestTypes.FriendListSize, 1, ct),
                grainFactory
                    .GetPlayerDailyTaskGrain((long)e.TargetPlayerId)
                    .ProgressAsync(QuestTypes.FriendListSize, 1, ct)
            )
            .ConfigureAwait(false);
}

/// <summary>Advances "PlaceItem" daily tasks.</summary>
public sealed class DailyTaskItemPlacedHandler(IGrainFactory grainFactory)
    : IEventHandler<ItemPlacedEvent>
{
    public async ValueTask HandleAsync(ItemPlacedEvent e, EventContext ctx, CancellationToken ct) =>
        await grainFactory
            .GetPlayerDailyTaskGrain(e.ActorPlayerId)
            .ProgressAsync(QuestTypes.PlaceItem, 1, ct)
            .ConfigureAwait(false);
}

/// <summary>Advances "CatalogPurchase" daily tasks.</summary>
public sealed class DailyTaskCatalogHandler(IGrainFactory grainFactory, ICommerceJournal journal)
    : IEventHandler<CatalogPurchasedEvent>
{
    public async ValueTask HandleAsync(
        CatalogPurchasedEvent e,
        EventContext ctx,
        CancellationToken ct
    )
    {
        // Its own guard key, not the quest handler's: two consumers of one event are two
        // deliveries to deduplicate, and sharing a key would mean whichever ran first silenced the
        // other.
        if (!await CommerceReplayGuard.FirstDeliveryAsync(journal, e.OperationId, "daily-task", ct))
        {
            return;
        }

        await grainFactory
            .GetPlayerDailyTaskGrain((long)e.PlayerId)
            .ProgressAsync(QuestTypes.CatalogPurchase, 1, ct)
            .ConfigureAwait(false);
    }
}
