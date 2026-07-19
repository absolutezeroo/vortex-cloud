using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Notifications;

[GenerateSerializer, Immutable]
public sealed record PetLevelNotificationEventMessageComposer : IComposer
{
    [Id(0)]
    public required int PetId { get; init; }

    [Id(1)]
    public required int NewLevel { get; init; }

    [Id(2)]
    public required string PetName { get; init; }
}
