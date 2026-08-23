using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Events.Registry;
using Vortex.Primitives.Events;
using Vortex.Primitives.Orleans;
using Vortex.Primitives.Quests;
using Vortex.Primitives.Quests.Grains;

namespace Vortex.Players.Quests.Events;

// Quest objective progression, driven by the same domain events as achievements. A quest advances
// when its quest_type matches the trigger name. The event pipeline isolates handler exceptions, so
// a progression failure never breaks the originating action.

/// <summary>Advances "RoomEntry" quests when the player enters a room, counting only distinct other
/// players' rooms (own room ignored, repeat visits don't re-count).</summary>
public sealed class QuestRoomEntryHandler(IGrainFactory grainFactory)
    : IEventHandler<PlayerEnteredRoomEvent>
{
    public async ValueTask HandleAsync(
        PlayerEnteredRoomEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await grainFactory
            .GetPlayerQuestGrain(e.PlayerId)
            .ProgressRoomVisitAsync(e.RoomId, e.EnteredAtUtc, ct)
            .ConfigureAwait(false);
}

/// <summary>Advances "FriendListSize" quests for both sides of an accepted friend request.</summary>
public sealed class QuestFriendHandler(IGrainFactory grainFactory)
    : IEventHandler<FriendRequestAcceptedEvent>
{
    public async ValueTask HandleAsync(
        FriendRequestAcceptedEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await Task.WhenAll(
                grainFactory
                    .GetPlayerQuestGrain((long)e.ActorPlayerId)
                    .ProgressAsync(QuestTypes.FriendListSize, 1, ct),
                grainFactory
                    .GetPlayerQuestGrain((long)e.TargetPlayerId)
                    .ProgressAsync(QuestTypes.FriendListSize, 1, ct)
            )
            .ConfigureAwait(false);
}

/// <summary>Advances "AvatarLooks" quests each time the player changes their figure.</summary>
public sealed class QuestFigureHandler(IGrainFactory grainFactory)
    : IEventHandler<PlayerFigureChangedEvent>
{
    public async ValueTask HandleAsync(
        PlayerFigureChangedEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await grainFactory
            .GetPlayerQuestGrain(e.PlayerId)
            .ProgressAsync(QuestTypes.AvatarLooks, 1, ct)
            .ConfigureAwait(false);
}

/// <summary>Advances "RespectGiven" quests (e.g. the social GIVERESPECT quest) for the giver.</summary>
public sealed class QuestRespectHandler(IGrainFactory grainFactory)
    : IEventHandler<RespectGivenEvent>
{
    public async ValueTask HandleAsync(
        RespectGivenEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await grainFactory
            .GetPlayerQuestGrain((long)e.ActorPlayerId)
            .ProgressAsync(QuestTypes.RespectGiven, 1, ct)
            .ConfigureAwait(false);
}

/// <summary>Advances "CatalogPurchase" quests when the player buys from the catalog. Passes the offer
/// id as the target so a quest can require a specific offer ("buy offer 12") or, with no target, any
/// purchase. Quantity advances the step count.</summary>
public sealed class QuestCatalogPurchaseHandler(IGrainFactory grainFactory)
    : IEventHandler<CatalogPurchasedEvent>
{
    public async ValueTask HandleAsync(
        CatalogPurchasedEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await grainFactory
            .GetPlayerQuestGrain(e.PlayerId)
            .ProgressAsync(
                QuestTypes.CatalogPurchase,
                e.Quantity > 0 ? e.Quantity : 1,
                QuestTypes.TargetOfferId,
                e.OfferId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ct
            )
            .ConfigureAwait(false);
}

/// <summary>Advances "Login" quests at most once per calendar day (reconnecting many times a day
/// still counts once).</summary>
public sealed class QuestLoginHandler(IGrainFactory grainFactory)
    : IEventHandler<PlayerLoggedInEvent>
{
    public async ValueTask HandleAsync(
        PlayerLoggedInEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await grainFactory
            .GetPlayerQuestGrain((long)e.PlayerId)
            .ProgressDailyAsync(QuestTypes.Login, 1, ct)
            .ConfigureAwait(false);
}

/// <summary>Advances "MottoChange" quests each time the player changes their motto.</summary>
public sealed class QuestMottoHandler(IGrainFactory grainFactory)
    : IEventHandler<PlayerMottoChangedEvent>
{
    public async ValueTask HandleAsync(
        PlayerMottoChangedEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await grainFactory
            .GetPlayerQuestGrain(e.PlayerId)
            .ProgressAsync(QuestTypes.MottoChange, 1, ct)
            .ConfigureAwait(false);
}

/// <summary>Advances "RespectReceived" quests each time the player receives a respect.</summary>
public sealed class QuestRespectReceivedHandler(IGrainFactory grainFactory)
    : IEventHandler<RespectReceivedEvent>
{
    public async ValueTask HandleAsync(
        RespectReceivedEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await grainFactory
            .GetPlayerQuestGrain((long)e.PlayerId)
            .ProgressAsync(QuestTypes.RespectReceived, 1, ct)
            .ConfigureAwait(false);
}

/// <summary>Advances "TradeCompleted" quests for both sides of a completed trade.</summary>
public sealed class QuestTradeHandler(IGrainFactory grainFactory)
    : IEventHandler<TradeCompletedEvent>
{
    public async ValueTask HandleAsync(
        TradeCompletedEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await Task.WhenAll(
                grainFactory
                    .GetPlayerQuestGrain((long)e.PlayerOneId)
                    .ProgressAsync(QuestTypes.TradeCompleted, 1, ct),
                grainFactory
                    .GetPlayerQuestGrain((long)e.PlayerTwoId)
                    .ProgressAsync(QuestTypes.TradeCompleted, 1, ct)
            )
            .ConfigureAwait(false);
}

/// <summary>Advances "CreateGroup" quests when the player creates a group.</summary>
public sealed class QuestCreateGroupHandler(IGrainFactory grainFactory)
    : IEventHandler<GroupCreatedEvent>
{
    public async ValueTask HandleAsync(
        GroupCreatedEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await grainFactory
            .GetPlayerQuestGrain((long)e.ActorPlayerId)
            .ProgressAsync(QuestTypes.CreateGroup, 1, ct)
            .ConfigureAwait(false);
}

/// <summary>Advances "JoinGroup" quests when the player joins a group.</summary>
public sealed class QuestJoinGroupHandler(IGrainFactory grainFactory)
    : IEventHandler<GroupMemberJoinedEvent>
{
    public async ValueTask HandleAsync(
        GroupMemberJoinedEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await grainFactory
            .GetPlayerQuestGrain((long)e.ActorPlayerId)
            .ProgressAsync(QuestTypes.JoinGroup, 1, ct)
            .ConfigureAwait(false);
}

/// <summary>Advances "BuyClub" quests when the player buys Habbo Club.</summary>
public sealed class QuestBuyClubHandler(IGrainFactory grainFactory)
    : IEventHandler<ClubPurchasedEvent>
{
    public async ValueTask HandleAsync(
        ClubPurchasedEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await grainFactory
            .GetPlayerQuestGrain((long)e.PlayerId)
            .ProgressAsync(QuestTypes.BuyClub, 1, ct)
            .ConfigureAwait(false);
}

/// <summary>Advances "PlaceItem" quests when the player places furniture. Passes the furniture
/// definition id as the target so a quest can require a specific furni type ("place a dragon lamp")
/// or, with no target, any placement.</summary>
public sealed class QuestPlaceItemHandler(IGrainFactory grainFactory)
    : IEventHandler<ItemPlacedEvent>
{
    public async ValueTask HandleAsync(ItemPlacedEvent e, EventContext ctx, CancellationToken ct) =>
        await grainFactory
            .GetPlayerQuestGrain((long)e.ActorPlayerId)
            .ProgressAsync(
                QuestTypes.PlaceItem,
                1,
                QuestTypes.TargetBaseItemId,
                e.DefinitionId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ct
            )
            .ConfigureAwait(false);
}
