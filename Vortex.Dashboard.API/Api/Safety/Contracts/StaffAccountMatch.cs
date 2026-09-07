using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>
/// One account the search matched.
/// </summary>
/// <param name="PlayerNames">Every habbo on the account, because an operator recognises the name
/// rather than the email it signed up with.</param>
/// <param name="RoleIds">What it already holds, so the form does not offer to grant it twice.</param>
public sealed record StaffAccountMatch(
    int Id,
    string Email,
    IReadOnlyList<string> PlayerNames,
    IReadOnlyList<int> RoleIds
);
