using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Fishing;

/// <summary>
/// The player's whole Hook Havoc attempt, sent once when it ends. Vortex-specific: no AS3 or Habbo
/// equivalent.
/// </summary>
/// <remarks>
/// <para>
/// A flat list of <c>tick</c> then <c>direction</c> pairs: -1 for Q, +1 for E. Flat rather than a
/// list of records because the wire has no framing for nested tuples and a pair is not worth
/// inventing one for. An odd-length list is malformed and the trailing value is ignored.
/// </para>
/// <para>
/// <strong>This is input, never a verdict.</strong> The server replays it against the seed it
/// issued; nothing here says whether the player won.
/// </para>
/// </remarks>
public record VortexHookHavocInputMessage : IMessageEvent
{
    public required int[] Timeline { get; init; }
}
