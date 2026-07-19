using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.StuffData;
using Vortex.Primitives.Rooms.Enums;

namespace Vortex.Primitives.Furniture.Admin;

/// <summary>Outcome of a furniture-definition admin write, mirroring the
/// success/error-code shape used by the catalog admin surface.</summary>
public sealed record FurnitureAdminResult(bool Success, int? Id, string? ErrorCode)
{
    public static FurnitureAdminResult Ok(int id) => new(true, id, null);

    public static FurnitureAdminResult Fail(string errorCode) => new(false, null, errorCode);
}

public sealed record FurnitureDefinitionUpsertSpec(
    int SpriteId,
    string Name,
    ProductType ProductType,
    FurnitureCategory FurniCategory,
    string Logic,
    int TotalStates,
    int Width,
    int Length,
    double StackHeight,
    bool CanStack,
    bool CanWalk,
    bool CanSit,
    bool CanLay,
    bool CanRecycle,
    bool CanTrade,
    bool CanGroup,
    bool CanSell,
    FurnitureUsageType UsagePolicy,
    string? ExtraData,
    StuffDataType StuffDataType
);
