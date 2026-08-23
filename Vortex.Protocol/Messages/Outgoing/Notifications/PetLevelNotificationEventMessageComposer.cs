using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Pets.Snapshots;

namespace Vortex.Protocol.Messages.Outgoing.Notifications;

[GenerateSerializer, Immutable]
public sealed record PetLevelNotificationEventMessageComposer : IComposer
{
    [Id(0)]
    public required int PetId { get; init; }

    [Id(1)]
    public required int NewLevel { get; init; }

    [Id(2)]
    public required string PetName { get; init; }

    /// <summary>The client draws the pet beside the congratulation, so the figure travels with it.</summary>
    [Id(3)]
    public required PetSnapshot Pet { get; init; }
}
