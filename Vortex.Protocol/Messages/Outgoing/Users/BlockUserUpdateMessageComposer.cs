using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Users;

[GenerateSerializer, Immutable]
public sealed record BlockUserUpdateMessageComposer : IComposer
{
    [Id(0)]
    public required int PlayerId { get; init; }

    [Id(1)]
    public required bool IsBlocked { get; init; }
}
