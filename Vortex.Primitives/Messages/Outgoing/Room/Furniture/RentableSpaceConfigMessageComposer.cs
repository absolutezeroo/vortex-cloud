using System.Collections.Generic;
using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Snapshots;

namespace Vortex.Primitives.Messages.Outgoing.Room.Furniture;

[GenerateSerializer, Immutable]
public sealed record RentableSpaceConfigMessageComposer : IComposer
{
    [Id(0)]
    public required int FurnitureId { get; init; }

    [Id(1)]
    public required bool IsConfigured { get; init; }

    [Id(2)]
    public required int Price { get; init; }

    [Id(3)]
    public required int CurrencyTypeId { get; init; }

    [Id(4)]
    public required int RentDurationSeconds { get; init; }

    [Id(5)]
    public required bool RequiresHc { get; init; }

    [Id(6)]
    public required IReadOnlyList<AvailableCurrencySnapshot> AvailableCurrencies { get; init; }
}
