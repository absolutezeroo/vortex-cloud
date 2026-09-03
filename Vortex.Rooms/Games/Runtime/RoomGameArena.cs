using System.Collections.Generic;
using Vortex.Primitives.Rooms.Games;
using Vortex.Primitives.Rooms.Games.Components;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Rooms.Games.Arena;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Games.Runtime;

/// <summary>
/// One game's arena, as a filtered view over the room's single item index. There is no second index
/// and no furni subscription: the room already maintains one index keyed by logic type AND by the
/// interfaces that type implements, which is exactly a capability lookup, so a placement, a pickup
/// or a definition swap is reflected here the instant the room records it.
/// <para>
/// Complexity: <see cref="ComponentsOf{T}"/> and <see cref="CountOf{T}"/> are O(components with that
/// capability in the room) — the arena's own size, never the room's furniture count — and
/// <see cref="OnTile{T}"/> is O(items stacked on that one tile). The per-step lookups games make
/// during a match are the tile one.
/// </para>
/// </summary>
internal sealed class RoomGameArena(ArenaHost host, RoomGrain roomGrain) : IGameArena
{
    private readonly ArenaHost _host = host;
    private readonly RoomGrain _roomGrain = roomGrain;

    /// <summary>Read from the host rather than captured, because the view has to exist before the
    /// module that names it does — the module is handed the context the view belongs to.</summary>
    public GameId Game => _host.Game is { } game ? game.Profile.Id : GameId.None;

    public ArenaId Id => _host.Id;

    /// <summary>
    /// Whether a component of this game belongs to THIS installation. For a game that forms one arena
    /// per room — every Habbo game — the partition is the constant "instance 0" and this compiles down
    /// to a comparison against zero; for a game that separates its playfields it is a dictionary hit.
    /// Either way no game ever scans the room.
    /// </summary>
    private bool Owns(IGameComponent component) =>
        component.Game == Game
        && _roomGrain.GameRuntime.PartitionOf(Game).InstanceOf(component.ObjectId)
            == _host.Id.Instance;

    public IReadOnlyList<TComponent> ComponentsOf<TComponent>()
        where TComponent : class, IGameComponent
    {
        List<TComponent> found = [];

        foreach (TComponent candidate in _roomGrain._state.ItemIndex.LogicsOf<TComponent>())
        {
            if (Owns(candidate))
            {
                found.Add(candidate);
            }
        }

        return found;
    }

    public int CountOf<TComponent>()
        where TComponent : class, IGameComponent
    {
        int count = 0;

        foreach (IRoomItem item in _roomGrain._state.ItemIndex.ItemsOf<TComponent>())
        {
            if (item.Logic is TComponent component && Owns(component))
            {
                count++;
            }
        }

        return count;
    }

    public TComponent? OnTile<TComponent>(int tileIdx)
        where TComponent : class, IGameComponent
    {
        TComponent? candidate = _roomGrain.MapModule.FirstLogicOnTile<TComponent>(tileIdx);

        return candidate is not null && Owns(candidate) ? candidate : null;
    }

    public List<int> TilesOf<TComponent>()
        where TComponent : class, IGameComponent
    {
        List<int> tiles = [];

        foreach (TComponent component in ComponentsOf<TComponent>())
        {
            tiles.Add(_roomGrain.MapModule.ToIdx(component.X, component.Y));
        }

        return tiles;
    }
}
