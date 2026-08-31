using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Fishing;

/// <summary>
/// A catch triggered Hook Havoc. Vortex-specific: no AS3 or Habbo equivalent.
/// </summary>
/// <remarks>
/// Hook Havoc is Origins' skill minigame: <strong>Q</strong> nudges the line left, <strong>E</strong>
/// right, and the needle has to stay centred while a green bar fills before time runs out.
///
/// <para><strong>The client plays it and the server replays it.</strong> A minigame this tight is
/// unplayable if the server streams the needle back at any real latency, and a client that simply
/// reported "I won" is trivially faked. So the parameters and the <see cref="Seed"/> go down, the
/// player's whole input timeline comes back up, and the server runs the same attempt against the
/// same seed to decide.</para>
///
/// <para>Field order is the contract with vortex-modern-client's
/// <c>VortexHookHavocStartedMessageParser</c>. Append-only.</para>
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record VortexHookHavocStartedMessageComposer : IComposer
{
    /// <summary>What the answering input names. One attempt is live at a time.</summary>
    [Id(0)]
    public required int AttemptId { get; init; }

    /// <summary>Drives the drift. Both ends run the same generator from it, or the replay disagrees.</summary>
    [Id(1)]
    public required int Seed { get; init; }

    [Id(2)]
    public required int DurationMs { get; init; }

    /// <summary>How fast the bar fills while centred, in hundredths of a percent per tick.</summary>
    [Id(3)]
    public required int FillRate { get; init; }

    /// <summary>How far off centre still counts as centred, in the needle's own units.</summary>
    [Id(4)]
    public required int Tolerance { get; init; }
}
