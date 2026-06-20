using Orleans;
using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Groupforums;

[GenerateSerializer, Immutable]
public sealed record ForumDataMessageComposer : IComposer
{
    [Id(0)]
    public required ForumSnapshot Forum { get; init; }
}
