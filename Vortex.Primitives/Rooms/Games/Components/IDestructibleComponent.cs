namespace Vortex.Primitives.Rooms.Games.Components;

/// <summary>
/// Arena furniture a game can break during a match and must restore before the next one — the Freeze
/// ice block. Restoration is the runtime's cleanup contract, not something a game remembers to do.
/// </summary>
public interface IDestructibleComponent : IGameComponent
{
    /// <summary>The state this component is restored to when a match is prepared.</summary>
    int IntactState { get; }
}
