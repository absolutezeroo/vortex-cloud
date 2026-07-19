using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Groupforums;

[GenerateSerializer, Immutable]
public sealed record UnreadForumsCountMessageComposer : IComposer
{
    [Id(0)]
    public required int UnreadForumsCount { get; init; }
}
