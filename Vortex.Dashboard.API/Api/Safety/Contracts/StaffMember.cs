using System;
using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>
/// One account holding at least one role.
/// </summary>
/// <param name="RoleIds">The ids, not just the names: an assignment is addressed by (accountId,
/// roleId), and resolving a name back to an id in the browser breaks the moment two roles are
/// renamed alike.</param>
public sealed record StaffMember(
    int Id,
    string Email,
    DateTime CreatedAt,
    IReadOnlyList<string> PlayerNames,
    IReadOnlyList<StaffPlayer> Players,
    IReadOnlyList<string> Roles,
    IReadOnlyList<int> RoleIds
);
