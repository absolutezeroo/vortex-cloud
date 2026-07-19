using System.Collections.Generic;

namespace Vortex.Primitives.Permissions;

/// <summary>
/// The canonical, code-declared set of authorization capabilities. Capabilities are fine-grained,
/// namespaced strings — the unit of authorization across both the game and the dashboard. Roles (in
/// the database) map to sets of these; an account's effective rights are the union of its roles'
/// capabilities. New features declare their own capability here so the set stays discoverable and
/// typo-safe instead of being scattered rank checks.
/// </summary>
public static class Capabilities
{
    /// <summary>Grants every capability. Reserved for the owner role.</summary>
    public const string Wildcard = "*";

    public static class Room
    {
        /// <summary>Build/decorate in any room, bypassing ownership and room-rights.</summary>
        public const string BuildAny = "room.build.any";

        /// <summary>Moderate (kick/mute/settings) in any room.</summary>
        public const string ModerateAny = "room.moderate.any";
    }

    public static class Moderation
    {
        public const string Kick = "moderation.kick";
        public const string Mute = "moderation.mute";
        public const string Alert = "moderation.alert";
        public const string Ban = "moderation.ban";
        public const string TradingLock = "moderation.trading_lock";

        /// <summary>Matches the WIN63 client's own distinct "chatlogsPermission" tool flag — reading
        /// a player's chat history is gated separately from being able to sanction them.</summary>
        public const string Chatlogs = "moderation.chatlogs";

        /// <summary>Matches the WIN63 client's own distinct "cfhPermission" tool flag — handling CFH
        /// tickets (pick/close/release/default-action) is gated separately from direct
        /// kick/mute/ban/alert actions.</summary>
        public const string Cfh = "moderation.cfh";
    }

    public static class Economy
    {
        public const string GrantCredits = "economy.credits.grant";
        public const string GrantActivityPoints = "economy.activitypoints.grant";
        public const string GrantItem = "economy.item.grant";
    }

    public static class Navigator
    {
        /// <summary>Toggle a room's "staff pick" flag in the navigator/guest-room card.</summary>
        public const string StaffPick = "navigator.staffpick.manage";
    }

    public static class Dashboard
    {
        public const string OverviewRead = "dashboard.overview.read";
        public const string AuditRead = "dashboard.audit.read";
        public const string EconomyRead = "dashboard.economy.read";
        public const string PlayersRead = "dashboard.players.read";
        public const string FurnitureRead = "dashboard.furniture.read";

        public const string OpsGrantCurrency = "dashboard.ops.currency.grant";
        public const string OpsGrantItem = "dashboard.ops.item.grant";
        public const string OpsKickPlayer = "dashboard.ops.player.kick";
        public const string OpsManageVouchers = "dashboard.ops.vouchers.manage";
        public const string OpsBanAccount = "dashboard.ops.player.ban";
        public const string OpsMutePlayer = "dashboard.ops.player.mute";
        public const string OpsTradingLock = "dashboard.ops.player.trading_lock";
        public const string OpsCfhManage = "dashboard.ops.cfh.manage";
        public const string OpsRoomsManage = "dashboard.ops.rooms.manage";
        public const string CatalogRead = "dashboard.catalog.read";
        public const string OpsCatalogManage = "dashboard.ops.catalog.manage";
        public const string OpsFurnitureManage = "dashboard.ops.furniture.manage";
        public const string GroupsRead = "dashboard.groups.read";
        public const string PetsRead = "dashboard.pets.read";
        public const string CfhRead = "dashboard.cfh.read";
        public const string CatalogPurchasesRead = "dashboard.catalog.purchases.read";
        public const string WiredRead = "dashboard.wired.read";
    }

    /// <summary>Every declared capability, for validation and dashboard enumeration.</summary>
    public static IReadOnlyCollection<string> All { get; } =
        new[]
        {
            Wildcard,
            Room.BuildAny,
            Room.ModerateAny,
            Moderation.Kick,
            Moderation.Mute,
            Moderation.Alert,
            Moderation.Ban,
            Moderation.TradingLock,
            Moderation.Chatlogs,
            Moderation.Cfh,
            Navigator.StaffPick,
            Economy.GrantCredits,
            Economy.GrantActivityPoints,
            Economy.GrantItem,
            Dashboard.OverviewRead,
            Dashboard.AuditRead,
            Dashboard.EconomyRead,
            Dashboard.PlayersRead,
            Dashboard.FurnitureRead,
            Dashboard.OpsGrantCurrency,
            Dashboard.OpsGrantItem,
            Dashboard.OpsKickPlayer,
            Dashboard.OpsManageVouchers,
            Dashboard.OpsBanAccount,
            Dashboard.OpsMutePlayer,
            Dashboard.OpsTradingLock,
            Dashboard.OpsCfhManage,
            Dashboard.OpsRoomsManage,
            Dashboard.CatalogRead,
            Dashboard.OpsCatalogManage,
            Dashboard.OpsFurnitureManage,
            Dashboard.GroupsRead,
            Dashboard.PetsRead,
            Dashboard.CfhRead,
            Dashboard.CatalogPurchasesRead,
            Dashboard.WiredRead,
        };
}
