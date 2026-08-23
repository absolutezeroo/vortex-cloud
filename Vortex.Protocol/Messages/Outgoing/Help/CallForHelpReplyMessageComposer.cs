using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Help;

[GenerateSerializer, Immutable]
public sealed record CallForHelpReplyMessageComposer : IComposer
{
    [Id(0)]
    public required string Message { get; init; }
}
