using System.Collections.Generic;
using Vortex.Primitives.Rooms.Games;
using Vortex.Primitives.Rooms.Games.Components;

namespace Vortex.Rooms.Games.Arena;

/// <summary>
/// One game's view of the furniture that makes up its playfield. It is a filtered view over the
/// room's single item index — not a second index — so it stays correct through placement, pickup and
/// a definition swap without any game subscribing to furni events, and it costs nothing to keep.
/// <para>
/// Queries are by capability, never by base item id: a game asks for "the goals", and whichever
/// class provides <see cref="IGoalComponent"/> answers.
/// </para>
/// </summary>
public interface IGameArena
{
    GameId Game { get; }

    /// <summary>Every component of this game playing <typeparamref name="TComponent"/>'s role. A
    /// materialised snapshot: safe to iterate across an await that changes the room.</summary>
    IReadOnlyList<TComponent> ComponentsOf<TComponent>()
        where TComponent : class, IGameComponent;

    /// <summary>How many there are, without materialising them.</summary>
    int CountOf<TComponent>()
        where TComponent : class, IGameComponent;

    /// <summary>This game's <typeparamref name="TComponent"/> on that tile, or null. Bounds-checked,
    /// so a raw index is safe to pass.</summary>
    TComponent? OnTile<TComponent>(int tileIdx)
        where TComponent : class, IGameComponent;

    /// <summary>The tile indices this game's <typeparamref name="TComponent"/>s occupy.</summary>
    List<int> TilesOf<TComponent>()
        where TComponent : class, IGameComponent;
}
