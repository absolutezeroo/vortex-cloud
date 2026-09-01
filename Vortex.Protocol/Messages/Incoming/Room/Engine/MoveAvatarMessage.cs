using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Room.Engine;

public record MoveAvatarMessage : IMessageEvent
{
    public required int TargetX { get; init; }

    public required int TargetY { get; init; }

    /// <summary>Altitude of the surface the player aimed at, in hundredths of a tile height, or
    /// null from a client that does not send it. A DEVIATION from the Flash protocol, which carries
    /// only (x, y): with a 3D height map one (x, y) holds several surfaces -- the floor under a
    /// platform and the platform's top -- and the server was left guessing which one was clicked.
    /// Hundredths because that is already the unit RoomPathingSystem keys altitudes by.</summary>
    public int? TargetZKey { get; init; }
}
