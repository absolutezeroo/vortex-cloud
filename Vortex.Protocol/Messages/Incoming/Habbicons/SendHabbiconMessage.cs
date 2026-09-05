using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Habbicons;

/// <summary>
/// Use a Habbicon inside a private conversation. The client sends it as a message of its own with a
/// confirmation id, exactly as it does for a line of text -- a Habbicon <em>is</em> a message here,
/// not a decoration on one.
/// </summary>
public sealed record SendHabbiconMessage : IMessageEvent
{
    /// <summary>The conversation, which for a one-to-one chat is the other player's id.</summary>
    public required int ChatId { get; init; }

    public required int HabbiconId { get; init; }

    /// <summary>Echoed back so the client can match its optimistic bubble to the delivered one.</summary>
    public required int ConfirmationId { get; init; }
}
