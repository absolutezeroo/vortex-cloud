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

/// <summary>Advances "RoomEntry" quests each time the player enters a room.</summary>
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
            .ProgressAsync(QuestTypes.RoomEntry, 1, ct)
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
