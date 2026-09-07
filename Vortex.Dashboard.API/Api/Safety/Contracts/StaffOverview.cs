using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>
/// Who can do what: the roles, who holds them, and the sanction ladder the mod tool offers.
/// </summary>
/// <param name="UngrantedCapabilities">Declared capabilities no role grants -- features nobody can
/// reach. Empty when a wildcard role exists, because that role covers everything and the list would
/// otherwise be noise on a hotel that only uses the owner role.</param>
/// <param name="AllCapabilities">Every declared capability grouped by its namespace, so the role
/// editor offers the real set instead of a free-text box that can store a key granting nothing.</param>
/// <param name="Wildcard">The key that means all of them.</param>
public sealed record StaffOverview(
    StaffTotals Totals,
    IReadOnlyList<StaffRole> Roles,
    IReadOnlyList<StaffMember> Staff,
    IReadOnlyList<SanctionPresetRow> Presets,
    IReadOnlyList<string> UngrantedCapabilities,
    bool WildcardExists,
    IReadOnlyList<CapabilityGroup> AllCapabilities,
    string Wildcard,
    IReadOnlyList<SanctionPresetKindOption> PresetKinds
);
