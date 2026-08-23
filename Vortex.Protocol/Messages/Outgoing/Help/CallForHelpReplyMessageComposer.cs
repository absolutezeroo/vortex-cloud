using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Help;

[GenerateSerializer, Immutable]
public sealed record CallForHelpReplyMessageComposer : IComposer
{
    [Id(0)]
    public required string Message { get; init; }
}
