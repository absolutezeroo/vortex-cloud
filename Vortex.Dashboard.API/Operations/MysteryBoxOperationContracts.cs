namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Request bodies for the dashboard's mystery box admin operations, each carrying a mandatory
/// audited <c>Reason</c>. <c>Color</c> must be one of the eight the client can render; on a prize it
/// may be empty, meaning the prize can drop from any box colour. <c>ExtraParam</c> carries the effect
/// spec (<c>effectId[:seconds[:subType]]</c>) or the club months (<c>months[:vip]</c>) and is unused
/// for floor/wall prizes.
/// </summary>
public sealed record CreateMysteryBoxPrizeRequest(
    string Pool,
    string Color,
    string ProductType,
    int FurnitureDefinitionId,
    string ExtraParam,
    int Weight,
    bool Enabled,
    string Reason
);

public sealed record UpdateMysteryBoxPrizeRequest(
    int PrizeId,
    string Pool,
    string Color,
    string ProductType,
    int FurnitureDefinitionId,
    string ExtraParam,
    int Weight,
    bool Enabled,
    string Reason
);

public sealed record DeleteMysteryBoxPrizeRequest(int PrizeId, string Reason);

public sealed record GrantMysteryBoxKeyRequest(int PlayerId, string Color, string Reason);

public sealed record GrantMysteryBoxRequest(
    int PlayerId,
    int FurnitureDefinitionId,
    string Color,
    string Reason
);

public sealed record ReloadMysteryBoxRequest(string Reason);
