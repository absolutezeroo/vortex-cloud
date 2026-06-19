using Orleans;
using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Outgoing.Users;

[GenerateSerializer, Immutable]
public sealed record GroupDetailsChangedMessageComposer : IComposer
{
    [Id(0)]
    public required int GroupId { get; init; }
}
