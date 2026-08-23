using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Help;

/// <summary>
/// The session is over.
/// </summary>
/// <remarks>
/// The reason decides what the requester sees, and zero is not a neutral default: the client reads
/// 0 as "your guide vanished" and anything else as "it is finished, please rate it". A session
/// closed on purpose must therefore never send 0.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record GuideSessionEndedMessageComposer : IComposer
{
    [Id(0)]
    public required int EndReason { get; init; }
}
