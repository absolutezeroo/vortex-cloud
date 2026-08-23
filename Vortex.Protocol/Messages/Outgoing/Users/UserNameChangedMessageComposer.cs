using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Users;

[GenerateSerializer, Immutable]
public sealed record UserNameChangedMessageComposer : IComposer
{
    [Id(0)]
    public required int WebId { get; init; }

    [Id(1)]
    public required int Id { get; init; }

    [Id(2)]
    public required string Name { get; init; }
}
