using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>
/// One role and what it grants.
/// </summary>
/// <param name="UnknownCapabilities">Keys this role holds that are not declared anywhere. Each
/// grants nothing at all -- the authorization check compares against the declared set -- so a role
/// can look powerful and be inert.</param>
/// <param name="Wildcard">Whether it holds the key that means everything.</param>
public sealed record StaffRole(
    int Id,
    string Key,
    string Name,
    int CapabilityCount,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<string> UnknownCapabilities,
    bool Wildcard,
    int Holders
);
