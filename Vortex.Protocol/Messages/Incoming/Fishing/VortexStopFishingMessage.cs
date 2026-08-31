using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Fishing;

/// <summary>
/// The player walked away or closed the panel. Vortex-specific: no AS3 or Habbo equivalent.
/// </summary>
/// <remarks>
/// Carries nothing: the server knows which session belongs to which player, and a client naming
/// somebody else's would be naming a thing it has no business knowing.
///
/// <para>Not required for correctness — a session also ends when the spot depletes, and the server
/// is free to end one for its own reasons. This only spares it simulating a session nobody is
/// watching.</para>
/// </remarks>
public record VortexStopFishingMessage : IMessageEvent;
