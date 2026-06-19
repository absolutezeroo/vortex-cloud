using Orleans;
using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Groupforums;

[GenerateSerializer, Immutable]
public sealed record ThreadMessagesMessageComposer : IComposer
{
    [Id(0)]
    public required ThreadMessagesPageSnapshot Page { get; init; }
}
