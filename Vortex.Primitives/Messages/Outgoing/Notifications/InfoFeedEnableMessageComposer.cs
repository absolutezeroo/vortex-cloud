using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Notifications;

[GenerateSerializer, Immutable]
public sealed record InfoFeedEnableMessageComposer : IComposer
{
    [Id(0)]
    public required bool Enabled { get; init; }
}
