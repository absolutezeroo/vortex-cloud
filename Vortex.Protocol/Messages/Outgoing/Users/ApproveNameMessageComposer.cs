using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Users;

[GenerateSerializer, Immutable]
public sealed record ApproveNameMessageComposer : IComposer
{
    [Id(0)]
    public int Result { get; init; }

    [Id(1)]
    public string ValidationInfo { get; init; } = string.Empty;
}
