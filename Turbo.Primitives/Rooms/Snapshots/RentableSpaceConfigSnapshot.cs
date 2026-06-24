using System.Collections.Generic;
using Orleans;

namespace Turbo.Primitives.Rooms.Snapshots;

/// <summary>
/// Current owner-configurable terms for a rentable-space item.
/// Returned by <c>IRentableSpaceGrain.GetConfigAsync</c> so the owner
/// widget can pre-populate its fields.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record RentableSpaceConfigSnapshot
{
    [Id(0)]
    public required int FurnitureId { get; init; }

    /// <summary>True when a <c>rentable_space_terms</c> row exists for this definition.</summary>
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
