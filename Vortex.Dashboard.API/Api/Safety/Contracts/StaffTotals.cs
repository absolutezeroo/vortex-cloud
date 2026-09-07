namespace Vortex.Dashboard.API.Api.Safety.Contracts;

/// <summary>The permission surface, counted.</summary>
public sealed record StaffTotals(
    int RoleCount,
    int StaffAccounts,
    int DeclaredCapabilities,
    int GrantedCapabilities,
    int UngrantedCapabilities,
    int PresetCount,
    int ActiveBans
);
