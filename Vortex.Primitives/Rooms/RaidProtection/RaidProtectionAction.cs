namespace Vortex.Primitives.Rooms.RaidProtection;

/// <summary>
/// What happens to a visitor whose arrival trips the detector. Values are the client's
/// (<c>ACTION_KICK = 0</c>, <c>ACTION_TEMPORARY_BAN = 1</c>); the ban's length is a separate
/// setting and is only read when this is <see cref="TemporaryBan" /> — the client greys the
/// duration dropdown out for <see cref="Kick" />.
/// </summary>
public enum RaidProtectionAction
{
    Kick = 0,
    TemporaryBan = 1,
}
