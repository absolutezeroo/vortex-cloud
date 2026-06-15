using System.Collections.Generic;

namespace Turbo.Primitives.Permissions;

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
    }

    public static class Economy
    {
        public const string GrantCredits = "economy.credits.grant";
        public const string GrantActivityPoints = "economy.activitypoints.grant";
        public const string GrantItem = "economy.item.grant";
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
        };
}
