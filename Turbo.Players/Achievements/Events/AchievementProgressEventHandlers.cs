using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Turbo.Events.Registry;
using Turbo.Primitives.Events;
using Turbo.Primitives.Orleans;
using Turbo.Primitives.Players.Grains;

namespace Turbo.Players.Achievements.Events;

// Achievement progression triggers, wired to domain events so gameplay subsystems stay decoupled
// from achievements. The event pipeline isolates handler exceptions, so a progression failure never
// breaks the originating action (login, room entry, profile edit).

/// <summary>
/// Advances the "Login" achievement on each successful authentication. One step per login; a
/// once-per-day guard is a later refinement.
/// </summary>
public sealed class AchievementLoginHandler(IGrainFactory grainFactory)
    : IEventHandler<PlayerLoggedInEvent>
{
    public async ValueTask HandleAsync(
        PlayerLoggedInEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await grainFactory
            .GetPlayerAchievementGrain((long)e.PlayerId)
            .ProgressDailyAsync(AchievementNames.Login, 1, ct)
            .ConfigureAwait(false);
}

/// <summary>Advances the "RoomEntry" achievement each time the player enters a room.</summary>
public sealed class AchievementRoomEntryHandler(IGrainFactory grainFactory)
    : IEventHandler<PlayerEnteredRoomEvent>
{
    public async ValueTask HandleAsync(
        PlayerEnteredRoomEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await grainFactory
            .GetPlayerAchievementGrain(e.PlayerId)
            .ProgressAsync(AchievementNames.RoomEntry, 1, ct)
            .ConfigureAwait(false);
}

/// <summary>Advances the "Motto" achievement each time the player changes their motto.</summary>
public sealed class AchievementMottoHandler(IGrainFactory grainFactory)
    : IEventHandler<PlayerMottoChangedEvent>
{
    public async ValueTask HandleAsync(
        PlayerMottoChangedEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await grainFactory
            .GetPlayerAchievementGrain(e.PlayerId)
            .ProgressAsync(AchievementNames.Motto, 1, ct)
            .ConfigureAwait(false);
}

/// <summary>Advances the "AvatarLooks" achievement each time the player changes their figure.</summary>
public sealed class AchievementFigureHandler(IGrainFactory grainFactory)
    : IEventHandler<PlayerFigureChangedEvent>
{
    public async ValueTask HandleAsync(
        PlayerFigureChangedEvent e,
        EventContext ctx,
        CancellationToken ct
    ) =>
        await grainFactory
            .GetPlayerAchievementGrain(e.PlayerId)
            .ProgressAsync(AchievementNames.AvatarLooks, 1, ct)
            .ConfigureAwait(false);
}

/// <summary>
/// Advances the "FriendCount" achievement for both sides of a newly accepted friend request — the
/// edge adds a friend to each. The two grain calls are independent, so they run concurrently.
/// </summary>
public sealed class AchievementFriendCountHandler(IGrainFactory grainFactory)
    : IEventHandler<FriendRequestAcceptedEvent>
{
    public async ValueTask HandleAsync(
        FriendRequestAcceptedEvent e,
        EventContext ctx,
        CancellationToken ct
    )
    {
        IPlayerAchievementGrain actor = grainFactory.GetPlayerAchievementGrain(
            (long)e.ActorPlayerId
        );
        IPlayerAchievementGrain target = grainFactory.GetPlayerAchievementGrain(
            (long)e.TargetPlayerId
        );

        await Task.WhenAll(
                actor.ProgressAsync(AchievementNames.FriendListSize, 1, ct),
                target.ProgressAsync(AchievementNames.FriendListSize, 1, ct)
            )
            .ConfigureAwait(false);
    }
}

/// <summary>Advances the "RoomDecoFurniCount" achievement for the player who placed furniture.</summary>
public sealed class AchievementFurniPlacedHandler(IGrainFactory grainFactory)
    : IEventHandler<ItemPlacedEvent>
{
    public async ValueTask HandleAsync(ItemPlacedEvent e, EventContext ctx, CancellationToken ct) =>
        await grainFactory
            .GetPlayerAchievementGrain((long)e.ActorPlayerId)
            .ProgressAsync(AchievementNames.RoomDecoFurniCount, 1, ct)
            .ConfigureAwait(false);
}
