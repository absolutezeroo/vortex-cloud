using Vortex.Primitives.Furniture.Enums;

namespace Vortex.Primitives.MysteryBox.Admin;

/// <summary>
/// Outcome of a mystery box admin write. Mirrors the quest admin result: the service is a plain
/// in-process singleton, not a grain, so no Orleans attributes.
/// </summary>
public sealed record MysteryBoxAdminResult(bool Success, int? Id, string? ErrorCode)
{
    public static MysteryBoxAdminResult Ok(int id) => new(true, id, null);

    public static MysteryBoxAdminResult Fail(string errorCode) => new(false, null, errorCode);
}

/// <summary>Create/update spec for a box colour registration.</summary>
public sealed record MysteryBoxDefinitionSpec(
    int FurnitureDefinitionId,
    string Color,
    bool Enabled
);

/// <summary>
/// Create/update spec for a prize row. <paramref name="Color"/> empty means the prize can drop from
/// any box colour; <paramref name="ExtraParam"/> carries the effect spec
/// (<c>effectId[:seconds[:subType]]</c>) or the club month count (<c>months[:vip]</c>).
/// </summary>
public sealed record MysteryBoxPrizeSpec(
    MysteryBoxPrizePool Pool,
    string Color,
    ProductType ProductType,
    int FurnitureDefinitionId,
    string ExtraParam,
    int Weight,
    bool Enabled
);
