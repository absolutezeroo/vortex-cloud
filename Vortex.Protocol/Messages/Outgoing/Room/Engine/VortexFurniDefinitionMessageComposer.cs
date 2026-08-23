using Orleans;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Protocol.Messages.Outgoing.Room.Engine;

/// <summary>
/// One <c>furniture_definitions</c> row, answering <c>VortexGetFurniDefinitionMessage</c> and
/// re-sent after every write so the editor redraws from what was stored rather than what was typed.
/// Vortex-specific: no AS3 or Habbo equivalent.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record VortexFurniDefinitionMessageComposer : IComposer
{
    [Id(0)]
    public required int DefinitionId { get; init; }

    [Id(1)]
    public required int SpriteId { get; init; }

    [Id(2)]
    public required string Name { get; init; }

    [Id(3)]
    public required ProductType ProductType { get; init; }

    [Id(4)]
    public required FurnitureCategory FurniCategory { get; init; }

    [Id(5)]
    public required string Logic { get; init; }

    [Id(6)]
    public required int TotalStates { get; init; }

    [Id(7)]
    public required int Width { get; init; }

    [Id(8)]
    public required int Length { get; init; }

    [Id(9)]
    public required int StackHeightHundredths { get; init; }

    [Id(10)]
    public required bool CanStack { get; init; }

    [Id(11)]
    public required bool CanWalk { get; init; }

    [Id(12)]
    public required bool CanSit { get; init; }

    [Id(13)]
    public required bool CanLay { get; init; }

    [Id(14)]
    public required bool CanRecycle { get; init; }

    [Id(15)]
    public required bool CanTrade { get; init; }

    [Id(16)]
    public required bool CanGroup { get; init; }

    [Id(17)]
    public required bool CanSell { get; init; }

    [Id(18)]
    public required FurnitureUsageType UsagePolicy { get; init; }

    [Id(19)]
    public required string ExtraData { get; init; }

    [Id(20)]
    public required StuffDataType StuffDataType { get; init; }

    /// <summary>Empty on success, otherwise the admin service's own error code
    /// (<c>definition_not_found</c>, <c>duplicate_sprite_type_category</c>, ...), shown verbatim.</summary>
    [Id(21)]
    public required string Error { get; init; }
}
