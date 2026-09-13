namespace Vortex.Primitives.Rooms.RaidProtection;

/// <summary>
/// The <c>resultCode</c> the save reply carries back.
/// </summary>
/// <remarks>
/// <para>
/// The client declares seven of these (0 to 6, <c>_SafeCls_4512</c> in AIR 1.0.31) and acts on
/// exactly one distinction: <c>0</c> closes the dialog, anything else leaves it open and redraws it
/// from the settings in the same packet. So <see cref="Ok" /> is client-confirmed and the six
/// failure names below are <b>ours</b> — the client's own constants are obfuscated and nothing in
/// the tree says what they mean.
/// </para>
/// <para>
/// The values still matter: staying inside 0-6 keeps us honest if the official meanings ever turn
/// up, and it keeps the audit log readable. Do not add an eighth.
/// </para>
/// </remarks>
public enum RaidProtectionSaveResult
{
    /// <summary>Saved. The only value whose meaning is read out of the client.</summary>
    Ok = 0,

    /// <summary>The actor may not manage this room's protection.</summary>
    NotAllowed = 1,

    /// <summary>No such room, or the actor is not in it.</summary>
    RoomNotFound = 2,

    /// <summary>A field carried a value the client's own dropdowns cannot produce.</summary>
    InvalidSettings = 3,

    /// <summary>Turning protection on during a live incident, without the client's confirmation.</summary>
    ConfirmationRequired = 4,

    /// <summary>Saving faster than the rate limit allows.</summary>
    TooFast = 5,

    /// <summary>Something failed on the way to the database.</summary>
    Failed = 6,
}
