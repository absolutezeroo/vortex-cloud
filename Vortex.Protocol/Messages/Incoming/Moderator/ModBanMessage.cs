using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Moderator;

public record ModBanMessage : IMessageEvent
{
    public required int UserId { get; init; }
    public required string Message { get; init; }
    public int TopicId { get; init; }
    public int SanctionTypeId { get; init; }

    /// <summary>Unconfirmed exact meaning against the client — observed true only for one specific
    /// sanction preset. Parsed but not currently acted upon (no IP/machine-ban infra exists).</summary>
    public bool IsSpecialFlag { get; init; }

    /// <summary>-1 when this action isn't tied to a CFH ticket (the client omits the field).</summary>
    public int IssueId { get; init; } = -1;
}
