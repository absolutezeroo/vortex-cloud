using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Room.Session;

[GenerateSerializer, Immutable]
public sealed record HanditemConfigurationMessageComposer : IComposer
{
    [Id(0)]
    public required bool IsHanditemControlBlocked { get; init; }
}
