namespace Vortex.Dashboard.API.Api.Catalogue.Contracts;

/// <summary>
/// One furniture definition, as the admin list shows it.
/// </summary>
/// <param name="Logic">What the thing does. A key the client does not know binds nothing and the
/// furniture is inert in the room, which nothing else on this page would show.</param>
/// <param name="VendingIds">The hand items a vending machine gives, semicolon-separated. Null on
/// every definition that is not one -- and on the vending machines nobody has configured yet, which
/// is most of them: no source anywhere says which drink a given machine gives.</param>
public sealed record FurnitureDefinitionRow(
    int Id,
    int SpriteId,
    string Name,
    int ProductType,
    string ProductTypeLabel,
    int FurniCategory,
    string FurniCategoryLabel,
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
    int UsagePolicy,
    string UsagePolicyLabel,
    string? ExtraData,
    int StuffDataType,
    string StuffDataTypeLabel,
    string? VendingIds,
    string? IconUrl
);
