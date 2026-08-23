using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Pets.Snapshots;

namespace Vortex.Protocol.Messages.Outgoing.Room.Pets;

[GenerateSerializer, Immutable]
public sealed record PetFigureUpdateMessageComposer : IComposer
{
    [Id(0)]
    public required int RoomIndex { get; init; }

    [Id(1)]
    public required int PetId { get; init; }

    /// <summary>Carries the figure block the client redraws the pet from.</summary>
    [Id(2)]
    public required PetSnapshot Pet { get; init; }

    [Id(3)]
    public bool HasSaddle { get; init; }

    [Id(4)]
    public bool IsRiding { get; init; }
}
