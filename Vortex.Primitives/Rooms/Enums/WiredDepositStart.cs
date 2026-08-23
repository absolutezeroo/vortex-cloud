namespace Vortex.Primitives.Rooms.Enums;

/// <summary>What opening a chest deposit did.</summary>
/// <remarks>
/// The client is told whether its trade screen is replacing one already up, and that has to be the
/// truth: `overridePreviousTrade` makes the model close and rebuild the previous trade before
/// opening this one. Sending it unconditionally ran that path on every first deposit.
/// </remarks>
public enum WiredDepositStart
{
    /// <summary>This player may not fill this chest, or it is not one.</summary>
    Refused = 0,

    /// <summary>A deposit is now open, and none was before.</summary>
    Opened = 1,

    /// <summary>A deposit is now open, and it took the place of one this player already had.</summary>
    Replaced = 2,

    /// <summary>
    /// Refused because the chest is locked -- told apart from <see cref="Refused"/> because it is
    /// the one refusal this client has a word for, so it is the one the player can be told about.
    /// </summary>
    RefusedLocked = 3,
}
