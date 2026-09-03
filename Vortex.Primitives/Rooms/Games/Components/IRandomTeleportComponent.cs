namespace Vortex.Primitives.Rooms.Games.Components;

/// <summary>
/// A tile that whisks whoever steps on it to a random other teleporter of the same game.
/// </summary>
public interface IRandomTeleportComponent : IGameComponent
{
    /// <summary>Whether landing here can chain onto a further hop. False for the <c>_exclude</c>
    /// variant, which is where the chain stops.</summary>
    bool ChainsOnArrival { get; }
}
