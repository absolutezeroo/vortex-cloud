using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Users;

[GenerateSerializer, Immutable]
public sealed record IgnoreResultMessageComposer : IComposer
{
    [Id(0)]
    public required int UserId { get; init; }

    [Id(1)]
    public required int ResultCode { get; init; }
}
