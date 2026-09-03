using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Logic.Furniture;

namespace Vortex.Primitives.Rooms.Games.Components;

/// <summary>
/// A piece of furniture playing a role in exactly one game. The furni is the physical object; the
/// component is the role it plays, and a furni claims a role by implementing one of the capability
/// interfaces beside this one (<see cref="ITeamGateComponent"/>, <see cref="IArenaTileComponent"/>,
/// <see cref="IGoalComponent"/>, …) rather than by having its base item id recognised somewhere in
/// the game logic.
/// <para>
/// This is the whole coupling between furniture and games: a component never calls a game, and no
/// game ever asks the room for "the Battle Banzai furniture". The furni raises a
/// <see cref="GameSignal"/> when it is walked on, used or detached; the runtime routes it to the
/// module that owns <see cref="Game"/>; the module pattern-matches on the capability it cares about.
/// </para>
/// </summary>
public interface IGameComponent : IFurnitureLogic
{
    /// <summary>The game this furni belongs to. Fixed at construction from the bound logic key.</summary>
    GameId Game { get; }

    RoomObjectId ObjectId { get; }

    int X { get; }

    int Y { get; }
}
