using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Protocol.Messages.Incoming.Room.Engine;

/// <summary>
/// Client rewrites one <c>furniture_definitions</c> row from the definition editor.
/// Vortex-specific: no AS3 or Habbo equivalent.
///
/// Unlike the placed-item editor there is no field mask: <c>IFurnitureAdminService.UpdateAsync</c>
/// takes a whole <c>FurnitureDefinitionUpsertSpec</c> and writes every column, so the client always
/// sends the complete row it is currently showing. The editor loads that row from the server first,
/// which is what makes sending it back whole safe.
/// </summary>
public record VortexApplyFurniDefinitionMessage : IMessageEvent
{
    public required int DefinitionId { get; init; }

    public required int SpriteId { get; init; }

    public required string Name { get; init; }

    public required ProductType ProductType { get; init; }

    public required FurnitureCategory FurniCategory { get; init; }

    /// <summary>The interaction: the logic class the room engine instantiates for this type.</summary>
    public required string Logic { get; init; }

    public required int TotalStates { get; init; }

    public required int Width { get; init; }

    public required int Length { get; init; }

    /// <summary>Stack height in hundredths — the wire has no double, matching the altitude
    /// convention used by the placed-item editor.</summary>
    public required int StackHeightHundredths { get; init; }

    public required bool CanStack { get; init; }

    public required bool CanWalk { get; init; }

    public required bool CanSit { get; init; }

    public required bool CanLay { get; init; }

    public required bool CanRecycle { get; init; }

    public required bool CanTrade { get; init; }

    public required bool CanGroup { get; init; }

    public required bool CanSell { get; init; }

    public required FurnitureUsageType UsagePolicy { get; init; }

    public required string ExtraData { get; init; }

    public required StuffDataType StuffDataType { get; init; }
}
