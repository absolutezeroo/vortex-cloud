namespace Vortex.Primitives.Rooms.Games.Components;

/// <summary>
/// One tile of the playfield. The set of these placed in a room IS the arena — Habbo has no bounding
/// box for either the Banzai patch or the Freeze rink — so the runtime's arena index is what a game
/// asks for its playable area instead of scanning the room.
/// </summary>
public interface IArenaTileComponent : IGameComponent;
