namespace Vortex.Primitives.Groups.Enums;

/// <summary>
/// The viewer's standing in a guild as the client's guild-details screen understands it.
/// AS3-verified against <c>HabboGroupDetailsData</c>: join/request buttons require
/// <see cref="NotMember"/>, the leave button and "you are a member" text require
/// <see cref="Member"/>, and the "membership pending" text shows on <see cref="RequestPending"/>.
/// </summary>
public enum GroupMembershipStatus
{
    NotMember = 0,
    Member = 1,
    RequestPending = 2,
}
