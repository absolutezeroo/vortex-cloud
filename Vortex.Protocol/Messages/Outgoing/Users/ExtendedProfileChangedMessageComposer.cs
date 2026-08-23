using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Users;

[GenerateSerializer, Immutable]
public sealed record ExtendedProfileChangedMessageComposer : IComposer
{
    [Id(0)]
    public required int UserId { get; init; }
}
