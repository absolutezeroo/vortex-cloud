using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Navigator;

[GenerateSerializer, Immutable]
public sealed record DoorbellMessageComposer : IComposer
{
    [Id(0)]
    public string? Username { get; init; }
}
