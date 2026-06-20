using Orleans;
using Turbo.Primitives.Groups.Snapshots;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Groupforums;

[GenerateSerializer, Immutable]
public sealed record ForumsListMessageComposer : IComposer
{
    [Id(0)]
    public required ForumsListPageSnapshot Page { get; init; }
}
