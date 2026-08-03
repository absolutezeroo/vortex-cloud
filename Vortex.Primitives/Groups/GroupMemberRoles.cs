namespace Vortex.Primitives.Groups;

/// <summary>
/// The <see cref="Snapshots.GroupMemberSnapshot.RoleType"/> values, mirroring the client's member
/// enum. These are wire constants: the client switches on them to pick the row icon and the actions
/// it offers, so they cannot be renumbered to suit the server.
/// </summary>
public static class GroupMemberRoles
{
    public const int Owner = 0;
    public const int Admin = 1;
    public const int Member = 2;
    public const int Requested = 3;
    public const int Blocked = 4;
}
