using Orleans;
using Vortex.Primitives.Furniture.Enums;

namespace Vortex.Primitives.MysteryBox.Snapshots;

/// <summary>One weighted entry of a prize pool (an enabled <c>mystery_box_prizes</c> row).</summary>
[GenerateSerializer, Immutable]
public sealed record MysteryBoxPrizeSnapshot
{
    [Id(0)]
    public required int Id { get; init; }

    [Id(1)]
    public required MysteryBoxPrizePool Pool { get; init; }

    /// <summary>Box colour this prize is restricted to; empty means any colour.</summary>
    [Id(2)]
    public required string Color { get; init; }

    [Id(3)]
    public required ProductType ProductType { get; init; }

    [Id(4)]
    public required int FurnitureDefinitionId { get; init; }

    [Id(5)]
    public required string ExtraParam { get; init; }

    [Id(6)]
    public required int Weight { get; init; }
}
