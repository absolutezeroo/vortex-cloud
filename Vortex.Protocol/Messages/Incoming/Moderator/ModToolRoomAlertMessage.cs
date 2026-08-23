using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Moderator;

/// <summary>
/// The room tool's "Send caution" / "Send message" buttons: one line delivered to everybody in the
/// room the moderator has open.
/// </summary>
/// <remarks>
/// <para>
/// The client sends no room id with this (<c>_SafeCls_3239(actionType, message, "")</c>), and the
/// room tool can be opened for any room — from a chatlog row or a room-visit row, not only the one
/// the moderator is standing in. The target is therefore the last room this session asked about
/// through <c>GetModeratorRoomInfo</c>, which <c>RoomToolCtrl.show()</c> always sends before the
/// dialog that can raise this can exist. That ordering is read from the client; the server-side
/// choice to key the target off it is an inference, and the one place in this flow that is.
/// </para>
/// <para>
/// When any of the room-tool checkboxes are also ticked the client follows this with
/// <c>ModerateRoom</c>, which does carry the room id — so a mismatch is detectable there, and the
/// kick half of <see cref="ActionType"/> is applied by that message rather than by this one.
/// </para>
/// </remarks>
public record ModToolRoomAlertMessage : IMessageEvent
{
    /// <summary>
    /// One of the client's four constants: 0 = caution, 1 = caution with a kick following,
    /// 3 = message, 4 = message with a kick following. Only the caution/message distinction is
    /// acted on here — see <see cref="IsCaution"/>.
    /// </summary>
    public required int ActionType { get; init; }

    public required string Message { get; init; }

    /// <summary>
    /// Whether this goes out as a caution (a modal the recipient must dismiss) rather than an
    /// ordinary staff message. Values 0 and 1 are the caution pair, 3 and 4 the message pair.
    /// </summary>
    public bool IsCaution => ActionType is 0 or 1;
}
