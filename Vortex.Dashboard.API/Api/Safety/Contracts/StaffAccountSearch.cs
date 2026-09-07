using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>
/// Account search for the role-assignment form.
/// </summary>
/// <remarks>
/// Roles hang off the account, not the player, so the ordinary player picker cannot drive this: it
/// hands back a player id, and two players can share one account.
/// </remarks>
public sealed record StaffAccountSearch(int Count, IReadOnlyList<StaffAccountMatch> Items);
