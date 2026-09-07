using System;
using System.Collections.Generic;

namespace Vortex.Dashboard.API.Api.Platform.Contracts;

/// <summary>
/// Everything the player page and the entity popup show about one account.
/// </summary>
/// <param name="Online">Asked of the session gateway, not of <c>players.status</c>: nothing writes
/// that column after account creation, so it answers "offline" for a connected player. The page
/// gates its kick button on this.</param>
/// <param name="Window">The span the timeline and the activity counts cover; the panels above them
/// are current regardless.</param>
public sealed record PlayerProfile(
    int Id,
    string Name,
    string? Motto,
    string Figure,
    string? AvatarUrl,
    bool Online,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string Gender,
    string Perks,
    ProfileWindow Window,
    ProfileRooms OwnedRooms,
    IReadOnlyList<ProfileWallet> Wallets,
    ProfileInventory Inventory,
    ProfileActivity Activity,
    ProfileTimeline Timeline
);
