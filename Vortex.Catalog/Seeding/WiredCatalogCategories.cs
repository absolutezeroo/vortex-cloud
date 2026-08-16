using System;
using System.Collections.Generic;
using System.Linq;

namespace Vortex.Catalog.Seeding;

/// <summary>
/// Which page of the catalogue's wired section a wired furni belongs on, read from its classname.
/// </summary>
/// <remarks>
/// The classname prefix is the client's own taxonomy — a box's prefix decides which dialog the
/// client opens for it — so it is also the only classification that cannot drift from what the box
/// actually is. Pages are matched and created by <see cref="Localization"/>, which is what the
/// client keys its page names off, not by display name.
/// </remarks>
public sealed record WiredCatalogCategory(
    string ClassNamePrefix,
    string Localization,
    string Name,
    int SortOrder
);

public static class WiredCatalogCategories
{
    /// <summary>The localization of the catalogue page the wired section hangs from.</summary>
    public const string RootLocalization = "wired_furniture";

    /// <summary>
    /// The six families, in the order a builder meets them: something happens, something is done,
    /// something is checked, over these targets, remembering this, tweaked by that.
    /// </summary>
    /// <remarks>
    /// The first four localizations are the ones an imported Habbo catalogue already uses, so an
    /// existing hotel keeps its pages instead of growing a second set beside them.
    /// </remarks>
    public static readonly IReadOnlyList<WiredCatalogCategory> All =
    [
        new("wf_trg_", "triggers", "Triggers", 1),
        new("wf_act_", "effects", "Effects", 2),
        new("wf_cnd_", "conditions", "Conditions", 3),
        new("wf_slc_", "selectors", "Selectors", 4),
        new("wf_var_", "variables", "Variables", 5),
        new("wf_xtra_", "wired_addons", "Add-ons", 6),
    ];

    /// <summary>The category this classname belongs to, or null when it is not one of the six
    /// families — the decorative wired furni (wires, plates, tokens) are sold as ordinary furniture
    /// and are none of this seeder's business.</summary>
    public static WiredCatalogCategory? ForClassName(string? className) =>
        string.IsNullOrEmpty(className)
            ? null
            : All.FirstOrDefault(category =>
                className.StartsWith(category.ClassNamePrefix, StringComparison.Ordinal)
            );
}
