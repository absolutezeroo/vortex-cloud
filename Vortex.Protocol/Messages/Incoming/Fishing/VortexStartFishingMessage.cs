using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Object;

namespace Vortex.Protocol.Messages.Incoming.Fishing;

/// <summary>
/// The player clicked a fish shadow and wants to start fishing there. Vortex-specific: no AS3 or
/// Habbo equivalent.
/// </summary>
/// <remarks>
/// The only thing the client decides about a session. Everything after this — which fish, what
/// weight, what rewards, when the spot runs dry — is the server's, and arrives unasked.
///
/// <para>It names the spot and nothing else: the room comes from the session, and a client naming
/// its own player id would be naming a thing the server already knows better.</para>
/// </remarks>
public record VortexStartFishingMessage : IMessageEvent
{
    public required RoomObjectId SpotObjectId { get; init; }
}
