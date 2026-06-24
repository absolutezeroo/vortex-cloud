using Orleans;
using Turbo.Primitives.Rooms.Enums;

namespace Turbo.Primitives.Rooms.Snapshots;

/// <summary>
/// Projection of <c>room_rentable_spaces</c> + terms sent in the status composer.
/// Field set matches the WIN63 wire mapping (DATA-MODEL §3.2).
/// </summary>
[GenerateSerializer, Immutable]
public sealed record RentableSpaceStatusSnapshot
{
    [Id(0)]
    public required bool Rented { get; init; }

    [Id(1)]
    public required RentableSpaceRentFailedType CanRentErrorCode { get; init; }

    [Id(2)]
    public required int RenterId { get; init; }

    [Id(3)]
    public required string RenterName { get; init; }

    [Id(4)]
    public required int TimeRemaining { get; init; }

    [Id(5)]
    public required int Price { get; init; }

    [Id(6)]
    public required string CurrencyName { get; init; }
}
