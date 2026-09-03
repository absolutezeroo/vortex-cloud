using System;
using Vortex.Rooms.Games.Abstractions;
using Vortex.Rooms.Games.Runtime;

namespace Vortex.Rooms.Providers;

/// <summary>
/// Holds the games contributed by scanned assemblies and builds a fresh set per room, the same shape
/// as <see cref="RoomEventListenerProvider"/>: a game module is per-room state, so one instance can
/// never be shared across rooms.
/// <para>
/// It lives in <c>Vortex.Rooms</c> rather than in <c>Vortex.Primitives</c> beside the other room
/// providers because a game module is defined in terms of the games framework, which lives here. An
/// assembly contributing a game already references this one.
/// </para>
/// </summary>
public interface IRoomGameProvider
{
    IDisposable RegisterGame(
        IServiceProvider sp,
        Func<IServiceProvider, IRoomGameContext, IRoomGame> factory
    );

    /// <summary>Builds one instance of every registered game and plugs it into that room's runtime.</summary>
    void AttachGamesTo(RoomGameRuntime runtime);
}
