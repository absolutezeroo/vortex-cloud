using System.Collections.Generic;
using Turbo.Primitives.Permissions;

namespace Turbo.Authentication.Permissions;

/// <summary>
/// Default role bootstrap. Seeded only into a fresh database (see <see cref="PermissionSeederService"/>);
/// once a role exists its capabilities are administrator-managed and no longer overwritten.
/// </summary>
internal static class DefaultRoles
{
    public const string OwnerKey = "owner";

    public sealed record RoleSeed(string Key, string Name, IReadOnlyList<string> Capabilities);

    public static readonly IReadOnlyList<RoleSeed> All =
    [
        new RoleSeed("player", "Player", []),
        new RoleSeed(
            "moderator",
            "Moderator",
            [
                Capabilities.Moderation.Kick,
                Capabilities.Moderation.Mute,
                Capabilities.Moderation.Alert,
                Capabilities.Moderation.TradingLock,
                Capabilities.Moderation.Chatlogs,
                Capabilities.Room.ModerateAny,
                Capabilities.Dashboard.OverviewRead,
                Capabilities.Dashboard.AuditRead,
                Capabilities.Dashboard.PlayersRead,
                Capabilities.Dashboard.OpsKickPlayer,
            ]
        ),
        new RoleSeed(
            "economy",
            "Economy staff",
            [
                Capabilities.Economy.GrantCredits,
                Capabilities.Economy.GrantActivityPoints,
                Capabilities.Economy.GrantItem,
                Capabilities.Dashboard.OverviewRead,
                Capabilities.Dashboard.AuditRead,
                Capabilities.Dashboard.EconomyRead,
                Capabilities.Dashboard.PlayersRead,
                Capabilities.Dashboard.FurnitureRead,
                Capabilities.Dashboard.OpsGrantCurrency,
                Capabilities.Dashboard.OpsGrantItem,
                Capabilities.Dashboard.OpsManageVouchers,
            ]
        ),
        new RoleSeed(
            "admin",
            "Administrator",
            [
                Capabilities.Room.BuildAny,
                Capabilities.Room.ModerateAny,
                Capabilities.Moderation.Kick,
                Capabilities.Moderation.Mute,
                Capabilities.Moderation.Alert,
                Capabilities.Moderation.Ban,
                Capabilities.Moderation.TradingLock,
                Capabilities.Moderation.Chatlogs,
                Capabilities.Economy.GrantCredits,
                Capabilities.Economy.GrantActivityPoints,
                Capabilities.Economy.GrantItem,
                Capabilities.Dashboard.OverviewRead,
                Capabilities.Dashboard.AuditRead,
                Capabilities.Dashboard.EconomyRead,
                Capabilities.Dashboard.PlayersRead,
                Capabilities.Dashboard.FurnitureRead,
                Capabilities.Dashboard.OpsGrantCurrency,
                Capabilities.Dashboard.OpsGrantItem,
                Capabilities.Dashboard.OpsKickPlayer,
                Capabilities.Dashboard.OpsManageVouchers,
            ]
        ),
        new RoleSeed("owner", "Owner", [Capabilities.Wildcard]),
    ];
}
